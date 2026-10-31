using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace hyoteiten
{
    public partial class Form1 : Form
    {
        private string selectedFilePath = "";
        private bool isKmzMode = false;

        private List<List<Tuple<double, double, double>>> extractedPolygons = new List<List<Tuple<double, double, double>>>();

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Enabled = true;
            btnRun.Enabled = true;

            if (comboZone.SelectedIndex < 0)
            {
                comboZone.SelectedIndex = 8; // デフォルト第9系
            }
        }

        private void comboZone_SelectedIndexChanged(object sender, EventArgs e) { }
        private void numOuterPitch_ValueChanged(object sender, EventArgs e) { }
        private void numInnerCoverage_ValueChanged(object sender, EventArgs e) { }
        private void numToleranceM_ValueChanged(object sender, EventArgs e) { }
        private void lblFilePath_Click(object sender, EventArgs e) { }
        private void label3_Click(object sender, EventArgs e) { }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "KML/KMZ Files (*.kml;*.kmz)|*.kml;*.kmz|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        selectedFilePath = ofd.FileName;
                        lblFilePath.Text = Path.GetFileName(selectedFilePath);
                        isKmzMode = Path.GetExtension(selectedFilePath).ToLower() == ".kmz";

                        extractedPolygons = LoadPolygonsFromKmlOrKmz(selectedFilePath);
                        if (extractedPolygons.Count > 0)
                        {
                            string msg = $"ファイルを読み込みました ({(isKmzMode ? "KMZ" : "KML")})\r\n抽出ポリゴン数: {extractedPolygons.Count}";
                            if (extractedPolygons.Count > 1)
                            {
                                msg += "\r\n※複数のポリゴンが検出されました。本システムでは先頭の1つ目のみを処理対象とします。";
                            }
                            MessageBox.Show(msg, "読み込み完了", MessageBoxButtons.OK, extractedPolygons.Count > 1 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("エラー: 有効な Polygon 情報を取得できませんでした。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"ファイル読み込みエラー:\r\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void btnRun_Click(object sender, EventArgs e)
        {
            if (extractedPolygons == null || extractedPolygons.Count == 0)
            {
                MessageBox.Show("KML / KMZ ファイルが読み込まれていません。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedZone = comboZone.SelectedIndex >= 0 ? comboZone.SelectedIndex + 1 : 9;
            double outerPitch = (double)numOuterPitch.Value;       // 90m
            double innerMaxDist = (double)numInnerCoverage.Value;  // 190m
            double toleranceMeters = (double)numToleranceM.Value;  // 1m
            if (outerPitch <= 0 || innerMaxDist <= 0)
            {
                MessageBox.Show("GH・NHの設定値は0より大きい値にしてください。", "入力確認", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<PointResult> allOuterPoints = new List<PointResult>();
            List<PointResult> allInnerPoints = new List<PointResult>();
            List<PointResult> allCheckPoints = new List<PointResult>();

            List<string> pitchReportLogs = new List<string>();

            int outerSeq = 1, innerSeq = 1;

            var geoPoly = extractedPolygons[0];
            var planePoly = geoPoly.Select(p => LatLonToPlaneRect(p.Item1, p.Item2, p.Item3, selectedZone)).ToList();
            planePoly = EnsureClockwise(planePoly);

            ProgressForm progressDlg = new ProgressForm();
            progressDlg.Show(this);
            progressDlg.Refresh();

            btnRun.Enabled = false;

            try
            {
                await Task.Run(() =>
                {
                    // 1. 外部評定点 (GH) 自動配置
                    allOuterPoints = GenerateOuterPointsEvenSpaced(planePoly, outerPitch, toleranceMeters, ref outerSeq, pitchReportLogs);

                    // 2. 内部評定点 (NH) 自動配置（Minimax選定 ＆ 連番振り直し対応版）
                    allInnerPoints = GenerateInnerPointsGrid1m(planePoly, allOuterPoints, outerPitch, innerMaxDist, ref innerSeq);

                    // 3. 検証点 (KS) の初期配置（最低2点、NHが3点以上の場合はNHと同数）
                    int initialCheckCount = (allInnerPoints.Count <= 2) ? 2 : allInnerPoints.Count;
                    allCheckPoints = GenerateCheckPointsBalanced(geoPoly, selectedZone, allOuterPoints, allInnerPoints, initialCheckCount);
                });
            }
            finally
            {
                progressDlg.Close();
                progressDlg.Dispose();
                btnRun.Enabled = true;
            }

            using (var pitchDlg = new PitchReportForm(pitchReportLogs))
            {
                if (pitchDlg.ShowDialog(this) != DialogResult.OK) return;
            }

            using (var dlg = new CheckPointAdjustForm(allCheckPoints.Count, allInnerPoints.Count))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    if (dlg.AdjustedCount != allCheckPoints.Count)
                    {
                        int targetCheckCount = Math.Max(2, dlg.AdjustedCount);
                        ProgressForm recalculateDlg = new ProgressForm("検証点を調整計算中です...");
                        recalculateDlg.Show(this);
                        recalculateDlg.Refresh();

                        try
                        {
                            await Task.Run(() =>
                            {
                                allCheckPoints = GenerateCheckPointsBalanced(geoPoly, selectedZone, allOuterPoints, allInnerPoints, targetCheckCount);
                            });
                        }
                        finally
                        {
                            recalculateDlg.Close();
                            recalculateDlg.Dispose();
                        }
                    }
                }
                else return;
            }

            bool isSatisfied = false;

            while (!isSatisfied)
            {
                // 配置・手動編集後にNHとKSを個別に並べ替え、連番を振り直す。
                allInnerPoints = await Task.Run(() => PointRoutePlanner.OrderAndRename(allInnerPoints, "NH"));
                allCheckPoints = await Task.Run(() => PointRoutePlanner.OrderAndRename(allCheckPoints, "KS"));
                List<string> validationErrors = ValidatePlacement(planePoly, allOuterPoints, allInnerPoints, allCheckPoints, outerPitch, innerMaxDist);
                if (validationErrors.Count > 0)
                {
                    string errListMsg = string.Join("\r\n・", validationErrors);
                    DialogResult errDlgRes = MessageBox.Show(
                        $"配置ルール違反が検出されました:\r\n・{errListMsg}\r\n\r\n手動編集画面を開いて配置を修正しますか？",
                        "配置不整合エラー",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );

                    if (errDlgRes == DialogResult.Yes)
                    {
                        using (var editForm = new PointEditForm(allOuterPoints, allInnerPoints, allCheckPoints, outerPitch, innerMaxDist))
                        {
                            if (editForm.ShowDialog(this) == DialogResult.OK)
                            {
                                allOuterPoints = editForm.OuterPoints;
                                allInnerPoints = editForm.InnerPoints;
                                allCheckPoints = editForm.CheckPoints;
                                continue;
                            }
                            else return;
                        }
                    }
                }

                try
                {
                    string tempKmlPath = Path.Combine(Path.GetTempPath(), "標識自動配置_プレビュー確認.kml");
                    ExportToKml(tempKmlPath, selectedZone, allOuterPoints, allInnerPoints, allCheckPoints, innerMaxDist);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = tempKmlPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Google Earth 起動エラー: {ex.Message}", "注意", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                DialogResult confirmResult = MessageBox.Show(
                    "Google Earth で配置結果を表示しました。\r\n\r\n配置はこれでよろしいですか？\r\n\r\n[はい] : 確定して保存画面へ進む\r\n[いいえ] : 手動微調整（ドラッグ・追加・削除）を行う",
                    "配置確認",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (confirmResult == DialogResult.Yes)
                {
                    isSatisfied = true;
                }
                else
                {
                    using (var editForm = new PointEditForm(allOuterPoints, allInnerPoints, allCheckPoints, outerPitch, innerMaxDist))
                    {
                        if (editForm.ShowDialog(this) == DialogResult.OK)
                        {
                            allOuterPoints = editForm.OuterPoints;
                            allInnerPoints = editForm.InnerPoints;
                            allCheckPoints = editForm.CheckPoints;
                        }
                        else return;
                    }
                }
            }

            string defaultExt = isKmzMode ? ".kmz" : ".kml";
            string filterStr = isKmzMode ? "KMZ File (*.kmz)|*.kmz" : "KML File (*.kml)|*.kml";

            using (SaveFileDialog sfd = new SaveFileDialog() { Filter = filterStr, FileName = $"標識配置結果{defaultExt}" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        if (isKmzMode)
                            ExportToKmz(sfd.FileName, selectedZone, allOuterPoints, allInnerPoints, allCheckPoints, innerMaxDist);
                        else
                            ExportToKml(sfd.FileName, selectedZone, allOuterPoints, allInnerPoints, allCheckPoints, innerMaxDist);

                        string simPath = Path.ChangeExtension(sfd.FileName, ".sim");
                        ExportToSima(simPath, allOuterPoints, allInnerPoints, allCheckPoints);

                        MessageBox.Show("出力が完了しました！", "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"保存エラー:\r\n{ex.Message}", "保存失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        #region KML / KMZ パース処理

        private List<List<Tuple<double, double, double>>> LoadPolygonsFromKmlOrKmz(string path)
        {
            List<List<Tuple<double, double, double>>> result = new List<List<Tuple<double, double, double>>>();
            string kmlContent = "";

            if (isKmzMode)
            {
                using (ZipArchive archive = ZipFile.OpenRead(path))
                {
                    var entry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".kml", StringComparison.OrdinalIgnoreCase));
                    if (entry != null)
                    {
                        using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                        {
                            kmlContent = reader.ReadToEnd();
                        }
                    }
                }
            }
            else
            {
                kmlContent = File.ReadAllText(path, Encoding.UTF8);
            }

            if (string.IsNullOrEmpty(kmlContent)) return result;

            XDocument doc = XDocument.Parse(kmlContent);
            XNamespace ns = doc.Root.Name.Namespace;

            var polygonNodes = doc.Descendants(ns + "Polygon").ToList();
            foreach (var polyNode in polygonNodes)
            {
                var outerNode = polyNode.Descendants(ns + "outerBoundaryIs").Descendants(ns + "coordinates").FirstOrDefault()
                             ?? polyNode.Descendants(ns + "coordinates").FirstOrDefault();

                if (outerNode != null)
                {
                    var pts = ParseCoordinates(outerNode.Value);
                    if (pts.Count >= 3) result.Add(pts);
                }
            }

            return result;
        }

        private List<Tuple<double, double, double>> ParseCoordinates(string rawText)
        {
            var list = new List<Tuple<double, double, double>>();
            string[] tokens = rawText.Trim().Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var t in tokens)
            {
                string[] parts = t.Split(',');
                if (parts.Length >= 2)
                {
                    if (double.TryParse(parts[0], out double lon) && double.TryParse(parts[1], out double lat))
                    {
                        double alt = parts.Length >= 3 && double.TryParse(parts[2], out double z) ? z : 0.0;
                        list.Add(Tuple.Create(lat, lon, alt));
                    }
                }
            }

            if (list.Count > 1 && Math.Abs(list[0].Item1 - list.Last().Item1) < 1e-8 && Math.Abs(list[0].Item2 - list.Last().Item2) < 1e-8)
            {
                list.RemoveAt(list.Count - 1);
            }

            return list;
        }

        #endregion

        #region 外部評定点・内部評定点・検証点 自動配置ロジック（Minimax選定 ＆ 連番振り直し対応）

        private List<PointResult> GenerateOuterPointsEvenSpaced(
            List<Tuple<double, double, double>> poly,
            double targetPitch,
            double toleranceMeters,
            ref int seqNo,
            List<string> pitchLogs)
        {
            List<PointResult> result = new List<PointResult>();
            var keyVertices = FilterKeyVertices(poly, toleranceMeters);

            int n = keyVertices.Count;
            for (int i = 0; i < n; i++)
            {
                var pCurr = keyVertices[i];
                var pNext = keyVertices[(i + 1) % n];

                int startSeq = seqNo;

                result.Add(new PointResult($"GH{seqNo++}", pCurr.Item1, pCurr.Item2, pCurr.Item3, PointType.OuterControl));

                double edgeDist = GetDistance(pCurr, pNext);

                int segments = (int)Math.Ceiling(edgeDist / targetPitch);
                if (segments < 1) segments = 1;

                double actualPitch = edgeDist / segments;

                for (int j = 1; j < segments; j++)
                {
                    double ratio = (j * actualPitch) / edgeDist;

                    double ny = pCurr.Item1 + (pNext.Item1 - pCurr.Item1) * ratio;
                    double nx = pCurr.Item2 + (pNext.Item2 - pCurr.Item2) * ratio;
                    double nz = pCurr.Item3 + (pNext.Item3 - pCurr.Item3) * ratio;

                    result.Add(new PointResult($"GH{seqNo++}", ny, nx, nz, PointType.OuterControl));
                }

                int endSeq = seqNo;
                string endName = (i == n - 1) ? "GH1" : $"GH{endSeq}";

                pitchLogs.Add($"GH{startSeq} ～ {endName} 区間: 総延長 {Math.Round(edgeDist):F0}m → {segments}分割 (実ピッチ: {Math.Round(actualPitch):F0}m)");
            }

            return result;
        }

        private List<Tuple<double, double, double>> FilterKeyVertices(
            List<Tuple<double, double, double>> poly,
            double toleranceMeters)
        {
            var currentPoly = new List<Tuple<double, double, double>>(poly);
            bool changed = true;

            while (changed && currentPoly.Count > 3)
            {
                changed = false;
                int n = currentPoly.Count;

                for (int i = 0; i < n; i++)
                {
                    var pPrev = currentPoly[(i - 1 + n) % n];
                    var pCurr = currentPoly[i];
                    var pNext = currentPoly[(i + 1) % n];

                    double distFromLine = GetPerpendicularDistance(pCurr, pPrev, pNext);

                    if (distFromLine <= toleranceMeters)
                    {
                        currentPoly.RemoveAt(i);
                        changed = true;
                        break;
                    }
                }
            }

            return currentPoly;
        }

        /// <summary>
        /// 1m高密度メッシュによる内部点自動配置処理
        /// 1. 外部評定点(GH)から outerPitch (90m) 離れた格子点を候補プールとして確定
        /// 2. 候補の中からポリゴン重心に近い点を 1点目 とする
        /// 3. 残りの未カバー候補の中から、既存の全点(GH+NH)から最も遠い点(Minimax)を順番に追加
        /// 4. すべて決定した後に NH1, NH2, NH3... ときれいに連番を振り直す
        /// </summary>
        private List<PointResult> GenerateInnerPointsGrid1m(
            List<Tuple<double, double, double>> poly,
            List<PointResult> outerPts,
            double outerPitch,
            double innerMaxDist,
            ref int seqNo)
        {
            List<PointResult> inners = new List<PointResult>();
            if (outerPts.Count == 0) return inners;

            PointResult neGH = outerPts.OrderByDescending(p => p.Y + p.X).First();
            double originY = neGH.Y;
            double originX = neGH.X;

            double minY = poly.Min(p => p.Item1);
            double maxY = poly.Max(p => p.Item1);
            double minX = poly.Min(p => p.Item2);
            double maxX = poly.Max(p => p.Item2);

            List<Tuple<double, double>> fineSamples = new List<Tuple<double, double>>();

            int startI = (int)Math.Floor(minY - originY);
            int endI = (int)Math.Ceiling(maxY - originY);
            int startJ = (int)Math.Floor(minX - originX);
            int endJ = (int)Math.Ceiling(maxX - originX);

            for (int i = startI; i <= endI; i++)
            {
                for (int j = startJ; j <= endJ; j++)
                {
                    double sy = originY + i * 1.0;
                    double sx = originX + j * 1.0;
                    if (IsPointInPolygon(sy, sx, poly))
                    {
                        fineSamples.Add(Tuple.Create(sy, sx));
                    }
                }
            }

            List<Tuple<double, double>> ghCenters = outerPts.Select(p => Tuple.Create(p.Y, p.X)).ToList();

            // ステップ1: すべてのGHから outerPitch (90m) の円に入らない格子点を「内部評定点候補」として確定
            List<Tuple<double, double>> candidates = fineSamples.Where(s =>
                ghCenters.All(gh => GetDistance(s.Item1, s.Item2, gh.Item1, gh.Item2) > outerPitch)
            ).ToList();

            // 候補が一切ない場合は重心に1点強制配置
            if (candidates.Count == 0)
            {
                var centroid = CalculatePolygonCentroid(poly);
                inners.Add(new PointResult("NH1", centroid.Item1, centroid.Item2, 0.0, PointType.InnerControl));
                return inners;
            }

            // GHの円の外にある候補を覆うため、配置済みNHだけを保持する。
            List<Tuple<double, double>> activeCenters = new List<Tuple<double, double>>();

            // ステップ2: 候補の中から中央（ポリゴン重心）に一番近い点を 1点目 とする
            var centroidPt = CalculatePolygonCentroid(poly);
            Tuple<double, double> firstNh = candidates.OrderBy(c => GetDistance(c.Item1, c.Item2, centroidPt.Item1, centroidPt.Item2)).First();

            inners.Add(new PointResult("TEMP", firstNh.Item1, firstNh.Item2, 0.0, PointType.InnerControl));
            activeCenters.Add(firstNh);
            candidates.Remove(firstNh);

            // ステップ3: 190mの円に入らなかった残りの候補から、既存点から一番遠い（Minimax）点を順番に追加していく
            while (true)
            {
                List<Tuple<double, double>> uncoveredCandidates = candidates.Where(c =>
                    !activeCenters.Any(ac => GetDistance(c.Item1, c.Item2, ac.Item1, ac.Item2) <= innerMaxDist)
                ).ToList();

                if (uncoveredCandidates.Count == 0) break;

                Tuple<double, double> nextNh = null;
                double maxMinDist = -1.0;

                foreach (var uc in uncoveredCandidates)
                {
                    double minDist = activeCenters.Min(ac => GetDistance(uc.Item1, uc.Item2, ac.Item1, ac.Item2));
                    if (minDist > maxMinDist)
                    {
                        maxMinDist = minDist;
                        nextNh = uc;
                    }
                }

                if (nextNh != null)
                {
                    inners.Add(new PointResult("TEMP", nextNh.Item1, nextNh.Item2, 0.0, PointType.InnerControl));
                    activeCenters.Add(nextNh);
                    candidates.Remove(nextNh);
                }
                else
                {
                    break;
                }
            }

            // ステップ4: すべての配置が完了したあとで、NH1, NH2, NH3... と連番をきれいに振り直す
            for (int i = 0; i < inners.Count; i++)
            {
                inners[i].Name = $"NH{i + 1}";
            }

            seqNo += inners.Count;

            return inners;
        }

        private Tuple<double, double> CalculatePolygonCentroid(List<Tuple<double, double, double>> poly)
        {
            double accumulatedArea = 0.0;
            double centerX = 0.0;
            double centerY = 0.0;

            int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                var p1 = poly[i];
                var p2 = poly[(i + 1) % n];

                double temp = (p1.Item2 * p2.Item1 - p2.Item2 * p1.Item1);
                accumulatedArea += temp;
                centerX += (p1.Item2 + p2.Item2) * temp;
                centerY += (p1.Item1 + p2.Item1) * temp;
            }

            if (Math.Abs(accumulatedArea) < 1e-9)
            {
                return Tuple.Create(poly.Average(p => p.Item1), poly.Average(p => p.Item2));
            }

            accumulatedArea *= 3.0;
            return Tuple.Create(centerY / accumulatedArea, centerX / accumulatedArea);
        }

        private List<PointResult> GenerateCheckPointsBalanced(
            List<Tuple<double, double, double>> geoPoly,
            int zone,
            List<PointResult> outerPts,
            List<PointResult> innerPts,
            int targetCount)
        {
            List<PointResult> checkPts = new List<PointResult>();
            int minRequiredKS = Math.Max(2, targetCount);

            List<Tuple<double, double>> existingPoints = new List<Tuple<double, double>>();
            foreach (var gh in outerPts) existingPoints.Add(Tuple.Create(gh.Y, gh.X));
            foreach (var nh in innerPts) existingPoints.Add(Tuple.Create(nh.Y, nh.X));

            var poly = geoPoly.Select(p => LatLonToPlaneRect(p.Item1, p.Item2, p.Item3, zone)).ToList();

            double minY = poly.Min(p => p.Item1);
            double maxY = poly.Max(p => p.Item1);
            double minX = poly.Min(p => p.Item2);
            double maxX = poly.Max(p => p.Item2);

            List<Tuple<double, double>> samples = new List<Tuple<double, double>>();
            for (double y = minY; y <= maxY; y += 5.0)
            {
                for (double x = minX; x <= maxX; x += 5.0)
                {
                    if (IsPointInPolygon(y, x, poly))
                    {
                        double distToExisting = existingPoints.Min(ep => GetDistance(y, x, ep.Item1, ep.Item2));
                        if (distToExisting >= 3.0)
                        {
                            samples.Add(Tuple.Create(y, x));
                        }
                    }
                }
            }

            for (int k = 1; k <= minRequiredKS; k++)
            {
                if (samples.Count == 0) break;

                Tuple<double, double> bestPt = null;
                double maxMinDist = -1.0;

                foreach (var s in samples)
                {
                    double minDist = existingPoints.Min(ep => GetDistance(s.Item1, s.Item2, ep.Item1, ep.Item2));

                    if (minDist > maxMinDist)
                    {
                        maxMinDist = minDist;
                        bestPt = s;
                    }
                }

                if (bestPt != null)
                {
                    var ks = new PointResult($"KS{k}", bestPt.Item1, bestPt.Item2, 0.0, PointType.CheckPoint);
                    checkPts.Add(ks);
                    existingPoints.Add(Tuple.Create(bestPt.Item1, bestPt.Item2));

                    samples.Remove(bestPt);
                }
            }

            return checkPts;
        }

        #endregion

        #region 自動配置・手動編集後の妥当性検証（バリデーション機能）

        private List<string> ValidatePlacement(
            List<Tuple<double, double, double>> poly,
            List<PointResult> outerPts,
            List<PointResult> innerPts,
            List<PointResult> checkPts,
            double outerRadius,
            double maxCoverageDist)
        {
            List<string> errors = new List<string>();
            var allPts = outerPts.Concat(innerPts).Concat(checkPts).ToList();

            if (innerPts.Count < 1)
            {
                errors.Add("内部評定点 (NH) は最低1点以上必要です。");
            }
            if (checkPts.Count < 2)
            {
                errors.Add("検証点 (KS) は最低2点以上必要です。");
            }

            foreach (var pt in innerPts.Concat(checkPts))
            {
                if (!IsPointInPolygon(pt.Y, pt.X, poly))
                {
                    errors.Add($"点 [{pt.Name}] が対象区域の外側に配置されています。");
                }
            }

            for (int i = 0; i < allPts.Count; i++)
            {
                for (int j = i + 1; j < allPts.Count; j++)
                {
                    double dist = GetDistance(allPts[i].Y, allPts[i].X, allPts[j].Y, allPts[j].X);
                    if (dist < 1.0)
                    {
                        errors.Add($"点 [{allPts[i].Name}] と [{allPts[j].Name}] の距離が非常に近接（{dist:F2}m）または重複しています。");
                    }
                }
            }

            var gridOrigin = outerPts.OrderByDescending(p => p.Y + p.X).FirstOrDefault();
            double originY = gridOrigin != null ? gridOrigin.Y : poly[0].Item1;
            double originX = gridOrigin != null ? gridOrigin.X : poly[0].Item2;

            double minY = poly.Min(p => p.Item1);
            double maxY = poly.Max(p => p.Item1);
            double minX = poly.Min(p => p.Item2);
            double maxX = poly.Max(p => p.Item2);



            int startI = (int)Math.Floor(minY - originY);
            int endI = (int)Math.Ceiling(maxY - originY);
            int startJ = (int)Math.Floor(minX - originX);
            int endJ = (int)Math.Ceiling(maxX - originX);

            bool hasUncovered = false;
            for (int i = startI; i <= endI && !hasUncovered; i++)
            {
                for (int j = startJ; j <= endJ && !hasUncovered; j++)
                {
                    double sy = originY + i * 1.0;
                    double sx = originX + j * 1.0;
                    if (IsPointInPolygon(sy, sx, poly))
                    {
                        bool covered = outerPts.Any(gh =>
                            GetDistance(sy, sx, gh.Y, gh.X) <= outerRadius)
                            || innerPts.Any(nh =>
                            GetDistance(sy, sx, nh.Y, nh.X) <= maxCoverageDist);
                        if (!covered)
                        {
                            errors.Add($"GHの半径{outerRadius:F1}m、NHの半径{maxCoverageDist:F1}mで覆えていない場所があります（北方向座標: {sy:F1}、東方向座標: {sx:F1}）。");
                            hasUncovered = true;
                        }
                    }
                }
            }

            return errors;
        }

        private double GetPerpendicularDistance(Tuple<double, double, double> pt, Tuple<double, double, double> lineStart, Tuple<double, double, double> lineEnd)
        {
            double y0 = pt.Item1, x0 = pt.Item2;
            double y1 = lineStart.Item1, x1 = lineStart.Item2;
            double y2 = lineEnd.Item1, x2 = lineEnd.Item2;

            double dx = x2 - x1, dy = y2 - y1;
            double lenSq = dx * dx + dy * dy;

            if (lenSq < 1e-9) return GetDistance(pt.Item1, pt.Item2, lineStart.Item1, lineStart.Item2);

            double num = Math.Abs(dy * x0 - dx * y0 + x2 * y1 - y2 * x1);
            return num / Math.Sqrt(lenSq);
        }

        private double GetDistance(double y1, double x1, double y2, double x2)
        {
            double dy = y1 - y2, dx = x1 - x2;
            return Math.Sqrt(dy * dy + dx * dx);
        }

        private double GetDistance(Tuple<double, double, double> p1, Tuple<double, double, double> p2) => GetDistance(p1.Item1, p1.Item2, p2.Item1, p2.Item2);

        private bool IsPointInPolygon(double py, double px, List<Tuple<double, double, double>> poly)
        {
            bool inside = false;
            int n = poly.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((poly[i].Item1 > py) != (poly[j].Item1 > py)) &&
                    (px < (poly[j].Item2 - poly[i].Item2) * (py - poly[i].Item1) / (poly[j].Item1 - poly[i].Item1) + poly[i].Item2))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private List<Tuple<double, double, double>> EnsureClockwise(List<Tuple<double, double, double>> poly)
        {
            double sum = 0;
            for (int i = 0; i < poly.Count; i++)
            {
                var p1 = poly[i];
                var p2 = poly[(i + 1) % poly.Count];
                sum += (p2.Item2 - p1.Item2) * (p2.Item1 + p1.Item1);
            }
            if (sum < 0) poly.Reverse();
            return poly;
        }

        #endregion

        #region GRS80 国土地理院 厳密ガウス・クリュージェル投影（完全正規式）

        private const double A_GRS80 = 6378137.0;                  // 長半径 (m)
        private const double F_GRS80 = 1.0 / 298.257222101;        // 扁平率
        private const double M0_DEF = 0.9999;                      // 縮尺係数

        private static void GetZoneOrigin(int zone, out double lat0Deg, out double lon0Deg)
        {
            double[] lats = { 0, 33, 33, 36, 33, 36, 36, 36, 36, 36, 40, 44, 44, 26, 26, 26, 26, 20, 26, 20 };
            double[] lons = {
                0,
                129.5,                 // 1系
                131.0,                 // 2系
                132.33333333333334,    // 3系
                133.5,                 // 4系
                134.33333333333334,    // 5系
                136.0,                 // 6系
                137.16666666666666,    // 7系
                138.5,                 // 8系
                139.83333333333334,    // 9系
                140.83333333333334,    // 10系
                140.25,                // 11系
                142.25,                // 12系
                144.25,                // 13系
                142.0,                 // 14系
                127.5,                 // 15系
                124.0,                 // 16系
                131.0,                 // 17系
                138.0,                 // 18系
                154.0                  // 19系
            };

            lat0Deg = (zone >= 1 && zone <= 19) ? lats[zone] : 36.0;
            lon0Deg = (zone >= 1 && zone <= 19) ? lons[zone] : 139.83333333333334;
        }

        private static double CalS(double phi)
        {
            double a = A_GRS80;
            double f = F_GRS80;
            double e2 = f * (2.0 - f);
            double e4 = e2 * e2;
            double e6 = e4 * e2;
            double e8 = e6 * e2;

            double A = a * (1.0 - e2 / 4.0 - 3.0 * e4 / 64.0 - 5.0 * e6 / 256.0 - 175.0 * e8 / 16384.0);
            double B = a * (3.0 * e2 / 8.0 + 3.0 * e4 / 32.0 + 45.0 * e6 / 1024.0 + 105.0 * e8 / 4096.0);
            double C = a * (15.0 * e4 / 256.0 + 45.0 * e6 / 1024.0 + 525.0 * e8 / 16384.0);
            double D = a * (35.0 * e6 / 3072.0 + 175.0 * e8 / 12288.0);
            double E = a * (315.0 * e8 / 131072.0);

            return A * phi - B * Math.Sin(2.0 * phi) + C * Math.Sin(4.0 * phi) - D * Math.Sin(6.0 * phi) + E * Math.Sin(8.0 * phi);
        }

        private Tuple<double, double, double> LatLonToPlaneRect(double latDeg, double lonDeg, double alt, int zone)
        {
            GetZoneOrigin(zone, out double lat0Deg, out double lon0Deg);

            double phi = latDeg * Math.PI / 180.0;
            double lambda = lonDeg * Math.PI / 180.0;
            double phi0 = lat0Deg * Math.PI / 180.0;
            double lambda0 = lon0Deg * Math.PI / 180.0;

            double a = A_GRS80;
            double f = F_GRS80;
            double m0 = M0_DEF;

            double e2 = f * (2.0 - f);
            double ePrime2 = e2 / (1.0 - e2);

            double S0 = CalS(phi0);
            double S = CalS(phi);

            double t = Math.Tan(phi);
            double eta2 = ePrime2 * Math.Pow(Math.Cos(phi), 2);
            double l = lambda - lambda0;

            double l2 = l * l, l3 = l2 * l, l4 = l3 * l, l5 = l4 * l, l6 = l5 * l, l7 = l6 * l;
            double cosPhi = Math.Cos(phi);
            double sinPhi = Math.Sin(phi);

            double N_val = a / Math.Sqrt(1.0 - e2 * sinPhi * sinPhi);
            double T_val = t * t;
            double C_val = eta2;

            double y = m0 * (S - S0 + N_val * sinPhi * cosPhi * l2 / 2.0 +
                             N_val * sinPhi * Math.Pow(cosPhi, 3) * (5.0 - T_val + 9.0 * C_val + 4.0 * C_val * C_val) * l4 / 24.0 +
                             N_val * sinPhi * Math.Pow(cosPhi, 5) * (61.0 - 58.0 * T_val + T_val * T_val + 270.0 * C_val - 330.0 * T_val * C_val) * l6 / 720.0);

            double x = m0 * (N_val * cosPhi * l +
                             N_val * Math.Pow(cosPhi, 3) * (1.0 - T_val + C_val) * l3 / 6.0 +
                             N_val * Math.Pow(cosPhi, 5) * (5.0 - 18.0 * T_val + T_val * T_val + 14.0 * C_val - 58.0 * T_val * C_val) * l5 / 120.0 +
                             N_val * Math.Pow(cosPhi, 7) * (61.0 - 479.0 * T_val + 179.0 * T_val * T_val - Math.Pow(T_val, 3)) * l7 / 5040.0);

            return Tuple.Create(y, x, alt);
        }

        private Tuple<double, double> PlaneRectToLatLon(double y, double x, int zone)
        {
            GetZoneOrigin(zone, out double lat0Deg, out double lon0Deg);

            double phi0 = lat0Deg * Math.PI / 180.0;
            double lambda0 = lon0Deg * Math.PI / 180.0;

            double a = A_GRS80;
            double f = F_GRS80;
            double m0 = M0_DEF;

            double e2 = f * (2.0 - f);
            double ePrime2 = e2 / (1.0 - e2);

            double S0 = CalS(phi0);
            double S_prime = S0 + y / m0;

            double phi1 = phi0;
            for (int i = 0; i < 5; i++)
            {
                double S1 = CalS(phi1);
                double rho1 = a * (1.0 - e2) / Math.Pow(1.0 - e2 * Math.Pow(Math.Sin(phi1), 2), 1.5);
                phi1 += (S_prime - S1) / rho1;
            }

            double T1 = Math.Pow(Math.Tan(phi1), 2);
            double C1 = ePrime2 * Math.Pow(Math.Cos(phi1), 2);
            double N1 = a / Math.Sqrt(1.0 - e2 * Math.Pow(Math.Sin(phi1), 2));
            double R1 = a * (1.0 - e2) / Math.Pow(1.0 - e2 * Math.Pow(Math.Sin(phi1), 2), 1.5);
            double D = x / (N1 * m0);

            double D2 = D * D, D3 = D2 * D, D4 = D3 * D, D5 = D4 * D, D6 = D5 * D;

            double phi = phi1 - (N1 * Math.Tan(phi1) / R1) * (D2 / 2.0 - (5.0 + 3.0 * T1 + 10.0 * C1 - 4.0 * C1 * C1 - 9.0 * ePrime2) * D4 / 24.0 + (61.0 + 90.0 * T1 + 45.0 * T1 * T1) * D6 / 720.0);
            double lambda = lambda0 + (1.0 / Math.Cos(phi1)) * (D - (1.0 + 2.0 * T1 + C1) * D3 / 6.0 + (5.0 - 2.0 * C1 + 28.0 * T1 - 3.0 * C1 * C1 + 8.0 * ePrime2 + 24.0 * T1 * T1) * D5 / 120.0);

            return Tuple.Create(phi * 180.0 / Math.PI, lambda * 180.0 / Math.PI);
        }

        #endregion

        #region KML / KMZ & SIMA 出力

        private string BuildKmlContent(int zone, List<PointResult> outer, List<PointResult> inner, List<PointResult> check, double innerMaxCoverageDist)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
            sb.AppendLine("  <Document>");
            sb.AppendLine("    <name>標識自動配置結果</name>");

            AppendStyle(sb, "outerStyle", "ff0000ff", "http://maps.google.com/mapfiles/kml/pushpin/red-pushpin.png");
            AppendStyle(sb, "innerStyle", "ff00ff00", "http://maps.google.com/mapfiles/kml/pushpin/grn-pushpin.png");
            AppendStyle(sb, "checkStyle", "ffff0000", "http://maps.google.com/mapfiles/kml/pushpin/blue-pushpin.png");

            sb.AppendLine("    <Style id=\"dimLineYellow\"><LineStyle><color>ff00ffff</color><width>3</width></LineStyle></Style>");

            sb.AppendLine("    <Style id=\"dimLabelStyle\">");
            sb.AppendLine("      <IconStyle><scale>0.6</scale><Icon><href>http://maps.google.com/mapfiles/kml/shapes/placemark_circle.png</href></Icon></IconStyle>");
            sb.AppendLine("      <LabelStyle><scale>1.2</scale><color>ffffffff</color></LabelStyle>");
            sb.AppendLine("    </Style>");

            AppendFolder(sb, "外部評定点 (GH)", "outerStyle", outer, zone);
            AppendFolder(sb, "内部評定点 (NH)", "innerStyle", inner, zone);
            AppendFolder(sb, "検証点 (KS)", "checkStyle", check, zone);


            sb.AppendLine("    <Folder>");
            sb.AppendLine("      <name>寸法線（ピッチ寸法線）</name>");

            sb.AppendLine("      <Folder>");
            sb.AppendLine("        <name>外部評定点 ピッチ寸法線</name>");
            int outerCount = outer.Count;
            for (int i = 0; i < outerCount; i++)
            {
                var p1 = outer[i];
                var p2 = outer[(i + 1) % outerCount];

                double dist = GetDistance(p1.Y, p1.X, p2.Y, p2.X);
                var latLon1 = PlaneRectToLatLon(p1.Y, p1.X, zone);
                var latLon2 = PlaneRectToLatLon(p2.Y, p2.X, zone);

                sb.AppendLine("        <Placemark>");
                sb.AppendLine($"          <name>{p1.Name}-{p2.Name} ({Math.Round(dist):F0}m)</name>");
                sb.AppendLine("          <styleUrl>#dimLineYellow</styleUrl>");
                sb.AppendLine("          <LineString>");
                sb.AppendLine($"            <coordinates>{latLon1.Item2:F8},{latLon1.Item1:F8},{p1.Z:F3} {latLon2.Item2:F8},{latLon2.Item1:F8},{p2.Z:F3}</coordinates>");
                sb.AppendLine("          </LineString>");
                sb.AppendLine("        </Placemark>");

                double midY = (p1.Y + p2.Y) / 2.0;
                double midX = (p1.X + p2.X) / 2.0;
                double midZ = (p1.Z + p2.Z) / 2.0;
                var midLatLon = PlaneRectToLatLon(midY, midX, zone);

                sb.AppendLine("        <Placemark>");
                sb.AppendLine($"          <name>{Math.Round(dist):F0}m</name>");
                sb.AppendLine("          <styleUrl>#dimLabelStyle</styleUrl>");
                sb.AppendLine("          <Point>");
                sb.AppendLine($"            <coordinates>{midLatLon.Item2:F8},{midLatLon.Item1:F8},{midZ:F3}</coordinates>");
                sb.AppendLine("          </Point>");
                sb.AppendLine("        </Placemark>");
            }
            sb.AppendLine("      </Folder>");

            sb.AppendLine("    </Folder>");

            sb.AppendLine("  </Document>");
            sb.AppendLine("</kml>");
            return sb.ToString();
        }

        private void ExportToKml(string path, int zone, List<PointResult> outer, List<PointResult> inner, List<PointResult> check, double innerMaxDist)
        {
            File.WriteAllText(path, BuildKmlContent(zone, outer, inner, check, innerMaxDist), Encoding.UTF8);
        }

        private void ExportToKmz(string kmzPath, int zone, List<PointResult> outer, List<PointResult> inner, List<PointResult> check, double innerMaxDist)
        {
            string kmlString = BuildKmlContent(zone, outer, inner, check, innerMaxDist);
            using (var fs = new FileStream(kmzPath, FileMode.Create))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("doc.kml", CompressionLevel.Optimal);
                using (var writer = new StreamWriter(entry.Open(), Encoding.UTF8))
                {
                    writer.Write(kmlString);
                }
            }
        }

        private void AppendStyle(StringBuilder sb, string styleId, string colorAgbr, string iconUrl)
        {
            sb.AppendLine($"    <Style id=\"{styleId}\">");
            sb.AppendLine("      <IconStyle>");
            sb.AppendLine($"        <color>{colorAgbr}</color>");
            sb.AppendLine("        <scale>1.3</scale>");
            sb.AppendLine($"        <Icon><href>{iconUrl}</href></Icon>");
            sb.AppendLine("      </IconStyle>");
            sb.AppendLine("      <LabelStyle>");
            sb.AppendLine("        <scale>1.3</scale>");
            sb.AppendLine("      </LabelStyle>");
            sb.AppendLine("    </Style>");
        }

        private void AppendFolder(StringBuilder sb, string folderName, string styleId, List<PointResult> points, int zone)
        {
            sb.AppendLine("    <Folder>");
            sb.AppendLine($"      <name>{folderName}</name>");

            foreach (var pt in points)
            {
                var latLon = PlaneRectToLatLon(pt.Y, pt.X, zone);
                sb.AppendLine("      <Placemark>");
                sb.AppendLine($"        <name>{pt.Name}</name>");
                sb.AppendLine($"        <styleUrl>#{styleId}</styleUrl>");
                sb.AppendLine("        <Point>");
                sb.AppendLine($"          <coordinates>{latLon.Item2:F8},{latLon.Item1:F8},{pt.Z:F3}</coordinates>");
                sb.AppendLine("        </Point>");
                sb.AppendLine("      </Placemark>");
            }

            sb.AppendLine("    </Folder>");
        }

        private void ExportToSima(string path, List<PointResult> outer, List<PointResult> inner, List<PointResult> check)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("G00,04,標識自動配置座標,");
            sb.AppendLine("Z00,");
            sb.AppendLine("Z01,2,");
            sb.AppendLine("A00,");

            int pNum = 1;
            Action<List<PointResult>> writePoints = (pts) =>
            {
                foreach (var p in pts)
                {
                    sb.AppendLine($"A01,{pNum++},{p.Name},{p.Y:F3},{p.X:F3},{p.Z:F3},");
                }
            };

            writePoints(outer);
            writePoints(inner);
            writePoints(check);

            sb.AppendLine("END");
            File.WriteAllBytes(path, Encoding.GetEncoding(932).GetBytes(sb.ToString()));
        }

        #endregion
    }

    #region データ定義・ダイアログ群

    public enum PointType { OuterControl, InnerControl, CheckPoint }

    public class PointResult
    {
        public string Name { get; set; }
        public double Y { get; set; } // North
        public double X { get; set; } // East
        public double Z { get; set; }
        public PointType Type { get; set; }

        public PointResult(string name, double y, double x, double z, PointType type)
        {
            Name = name;
            Y = y;
            X = x;
            Z = z;
            Type = type;
        }
    }

    public class ProgressForm : Form
    {
        public ProgressForm(string message = "計算中です。しばらくお待ちください...")
        {
            this.Text = "処理中";
            this.Size = new Size(420, 160);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = false;
            this.Font = new Font("メイリオ", 11F, FontStyle.Regular, GraphicsUnit.Point);

            Label lblMsg = new Label()
            {
                Text = message,
                Location = new Point(20, 30),
                Size = new Size(360, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            ProgressBar progressBar = new ProgressBar()
            {
                Location = new Point(30, 70),
                Size = new Size(340, 20),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30
            };

            this.Controls.Add(lblMsg);
            this.Controls.Add(progressBar);
        }
    }

    public class PitchReportForm : Form
    {
        public PitchReportForm(List<string> logs)
        {
            this.Text = "外部評定点 (GH) 各区間実ピッチ一覧";
            this.Size = new Size(620, 440);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Font = new Font("メイリオ", 12F, FontStyle.Regular, GraphicsUnit.Point);

            Label lblInfo = new Label()
            {
                Text = "外周の主要区間ごとに均等分割を行った計算結果（実ピッチ）です。\r\nこの内容で処理を続行しますか？",
                Location = new Point(15, 15),
                Size = new Size(570, 55)
            };

            ListBox lstReport = new ListBox()
            {
                Location = new Point(15, 75),
                Size = new Size(575, 240),
                Font = new Font("メイリオ", 11F)
            };
            foreach (var log in logs)
            {
                lstReport.Items.Add(log);
            }

            Button btnOk = new Button() { Text = "次へ (プレビュー)", Location = new Point(270, 335), Size = new Size(180, 40), DialogResult = DialogResult.OK };
            Button btnCancel = new Button() { Text = "キャンセル", Location = new Point(460, 335), Size = new Size(130, 40), DialogResult = DialogResult.Cancel };

            this.Controls.Add(lblInfo);
            this.Controls.Add(lstReport);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }

    public class CheckPointAdjustForm : Form
    {
        public int AdjustedCount { get; private set; }

        private NumericUpDown numCount;
        private Button btnOk;
        private Button btnCancel;

        public CheckPointAdjustForm(int initialCount, int innerCount)
        {
            this.Text = "検証点 (KS) 数量確認・調整";
            this.Size = new Size(520, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("メイリオ", 12F, FontStyle.Regular, GraphicsUnit.Point);

            Label lblMsg = new Label()
            {
                Text = $"内部評定点に基づき、検証点 (KS) が {initialCount} 点配置されました。この数量で確定するか、必要に応じて配置数を増減してください。(最低2点)",
                Location = new Point(20, 20),
                Size = new Size(460, 90)
            };

            Label lblNum = new Label() { Text = "検証点 配置数:", Location = new Point(20, 128), AutoSize = true };
            numCount = new NumericUpDown()
            {
                Location = new Point(170, 125),
                Value = Math.Max(2, initialCount),
                Minimum = 2,
                Maximum = 1000,
                Width = 120,
                TextAlign = HorizontalAlignment.Center
            };

            btnOk = new Button() { Text = "プレビュー出力", Location = new Point(200, 185), Size = new Size(160, 40), DialogResult = DialogResult.OK };
            btnCancel = new Button() { Text = "キャンセル", Location = new Point(370, 185), Size = new Size(120, 40), DialogResult = DialogResult.Cancel };

            btnOk.Click += (s, e) => { AdjustedCount = (int)numCount.Value; };

            this.Controls.Add(lblMsg);
            this.Controls.Add(lblNum);
            this.Controls.Add(numCount);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }

    #endregion
}
namespace hyoteiten
{
    // 平面直線距離による、始点・終点自由の開いたルート。
    public static class PointRoutePlanner
    {
        public static List<PointResult> OrderAndRename(List<PointResult> points, string prefix)
        {
            // 元の番号によらず、同じ座標なら同じ入力順になるようにする。
            var sorted = points.OrderByDescending(p => p.Y).ThenBy(p => p.X).ToList();
            int n = sorted.Count;
            if (n == 0) return sorted;
            var distances = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    double dy = sorted[i].Y - sorted[j].Y;
                    double dx = sorted[i].X - sorted[j].X;
                    distances[i, j] = Math.Sqrt(dy * dy + dx * dx);
                }
            int[] route = n <= 18 ? Exact(distances, n) : Approximate(distances, n);
            // 逆順でも同距離。北側、同緯度なら西側の端から始める。
            if (route[0] > route[n - 1]) Array.Reverse(route);
            var result = route.Select(i => sorted[i]).ToList();
            for (int i = 0; i < n; i++) result[i].Name = prefix + (i + 1);
            return result;
        }

        private static int[] Exact(double[,] d, int n)
        {
            int states = 1 << n;
            var costs = new double[states * n];
            var previous = new sbyte[states * n];
            for (int i = 0; i < costs.Length; i++)
            {
                costs[i] = double.PositiveInfinity;
                previous[i] = -1;
            }
            // 全点を始点候補にする。
            for (int i = 0; i < n; i++) costs[(1 << i) * n + i] = 0;
            for (int mask = 1; mask < states; mask++)
                for (int end = 0; end < n; end++)
                {
                    if ((mask & (1 << end)) == 0) continue;
                    int index = mask * n + end;
                    if (double.IsPositiveInfinity(costs[index])) continue;
                    for (int next = 0; next < n; next++)
                    {
                        if ((mask & (1 << next)) != 0) continue;
                        int target = (mask | (1 << next)) * n + next;
                        double cost = costs[index] + d[end, next];
                        if (cost < costs[target])
                        {
                            costs[target] = cost;
                            previous[target] = (sbyte)end;
                        }
                    }
                }
            int remaining = states - 1, last = 0;
            for (int i = 1; i < n; i++)
                if (costs[remaining * n + i] < costs[remaining * n + last]) last = i;
            var route = new int[n];
            for (int position = n - 1; position >= 0; position--)
            {
                route[position] = last;
                int before = previous[remaining * n + last];
                remaining ^= 1 << last;
                last = before;
            }
            return route;
        }

        private static int[] Approximate(double[,] d, int n)
        {
            int[] best = null;
            double bestLength = double.PositiveInfinity;
            // 大量の点でも始点探索数を制限する。
            int starts = Math.Min(n, 32);
            for (int s = 0; s < starts; s++)
            {
                var route = new int[n];
                var used = new bool[n];
                route[0] = s * n / starts;
                used[route[0]] = true;
                for (int i = 1; i < n; i++)
                {
                    int next = -1;
                    for (int j = 0; j < n; j++)
                        if (!used[j] && (next < 0 || d[route[i - 1], j] < d[route[i - 1], next])) next = j;
                    route[i] = next;
                    used[next] = true;
                }
                // 開いたルートの2-opt。端点を含む区間も反転できる。
                for (int pass = 0; pass < 100; pass++)
                {
                    double bestDelta = -1e-8;
                    int from = -1, to = -1;
                    for (int i = 0; i < n - 1; i++)
                        for (int j = i + 1; j < n; j++)
                        {
                            double delta = 0;
                            if (i > 0) delta += d[route[i - 1], route[j]] - d[route[i - 1], route[i]];
                            if (j + 1 < n) delta += d[route[i], route[j + 1]] - d[route[j], route[j + 1]];
                            if (delta < bestDelta) { bestDelta = delta; from = i; to = j; }
                        }
                    if (from < 0) break;
                    Array.Reverse(route, from, to - from + 1);
                }
                double length = 0;
                for (int i = 1; i < n; i++) length += d[route[i - 1], route[i]];
                if (length < bestLength) { bestLength = length; best = route; }
            }
            return best;
        }
    }
}
