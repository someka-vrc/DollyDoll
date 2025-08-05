using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Splines;


namespace Somekasu.DollyDoll
{
    public static class BSplineFactory
    {
        /// <summary>
        /// 両端の点を通り、中間の点を制御点とするBスプライン補間点をサンプリングしてBezierKnot列を生成する
        /// </summary>
        /// <param name="controlPoints">制御点の位置リスト（両端は通過点、中間は制御点）</param>
        /// <param name="closed">ループするかどうか</param>
        /// <param name="sampleCount">サンプリング数</param>
        /// <returns>生成されたスプライン</returns>
        /// <exception cref="ArgumentNullException">controlPointsがnullの場合</exception>
        /// <exception cref="ArgumentException">制御点が4個未満の場合</exception>
        /// <exception cref="ArgumentOutOfRangeException">sampleCountが1未満の場合</exception>
        public static Spline Create(IList<float3> controlPoints, bool closed = false, int sampleCount = 50)
        {
            return Create(controlPoints, out _, closed, sampleCount);
        }

        /// <summary>
        /// 両端の点を通り、中間の点を制御点とするBスプライン補間点をサンプリングしてBezierKnot列を生成する
        /// </summary>
        /// <param name="controlPoints">制御点の位置リスト（両端は通過点、中間は制御点）</param>
        /// <param name="controlPointParameters">各制御点の弧長パラメータ（0～1）を出力</param>
        /// <param name="closed">ループするかどうか</param>
        /// <param name="sampleCount">サンプリング数</param>
        /// <returns>生成されたスプライン</returns>
        /// <exception cref="ArgumentNullException">controlPointsがnullの場合</exception>
        /// <exception cref="ArgumentException">制御点が4個未満の場合</exception>
        /// <exception cref="ArgumentOutOfRangeException">sampleCountが1未満の場合</exception>
        public static Spline Create(IList<float3> controlPoints, out float[] controlPointParameters, bool closed = false, int sampleCount = 50)
        {
            if (controlPoints == null)
                throw new ArgumentNullException(nameof(controlPoints));
            if (controlPoints.Count < 4)
                throw new ArgumentException("制御点は4個以上必要です", nameof(controlPoints));
            if (sampleCount < 1)
                throw new ArgumentOutOfRangeException(nameof(sampleCount), "サンプリング数は1以上である必要があります");

            var spline = new Spline();
            int n = controlPoints.Count;
            int degree = 3; // 4個以上保証されているのでCubic B-splineで固定

            List<float3> pts = new List<float3>(controlPoints);
            
            if (closed)
            {
                // クローズドの場合、周期的連続性のために制御点を拡張
                // 最初のdegree個を末尾に、最後のdegree個を先頭に追加
                List<float3> extendedPts = new List<float3>();
                
                // 最後のdegree個を先頭に追加
                for (int i = n - degree; i < n; i++)
                {
                    extendedPts.Add(controlPoints[i]);
                }
                
                // 元の制御点を追加
                extendedPts.AddRange(controlPoints);
                
                // 最初のdegree個を末尾に追加
                for (int i = 0; i < degree; i++)
                {
                    extendedPts.Add(controlPoints[i]);
                }
                
                pts = extendedPts;
            }

            for (int i = 0; i < sampleCount; i++)
            {
                float t;
                if (closed)
                {
                    // クローズドの場合：0 から n の範囲でパラメータを計算（最後のサンプルは除外してループ）
                    t = (float)i / sampleCount * n;
                }
                else
                {
                    // オープンの場合：0 から n-1 の範囲でパラメータを計算
                    t = (float)i / (sampleCount - 1) * (n - 1);
                }
                
                float3 p = DeBoor(pts, degree, t, closed);
                spline.Add(new BezierKnot(p), TangentMode.AutoSmooth);
            }
            spline.Closed = closed;
            
            // 制御点の弧長パラメータを計算
            controlPointParameters = CalculateControlPointParameters(spline, controlPoints, closed);
            
            return spline;
        }

        // De Boor's algorithm for Clamped B-spline (両端を通るBスプライン)
        private static float3 DeBoor(IList<float3> pts, int degree, float t, bool closed)
        {
            int n = pts.Count;
            int k = degree;
            
            if (closed)
            {
                // クローズドの場合：周期的Bスプライン
                int originalN = n - degree; // 元の制御点数
                
                // パラメータを0からoriginalNの範囲に正規化
                t = t % originalN;
                if (t < 0) t += originalN;
                
                // ノットベクトルを均等間隔で生成（周期的）
                float[] knots = new float[originalN + 2 * k + 1];
                for (int i = 0; i < knots.Length; i++)
                {
                    knots[i] = i - k;
                }
                
                // ノットスパンを見つける
                int span = k;
                while (span < originalN + k && t >= knots[span + 1])
                {
                    span++;
                }
                
                // 制御点を取得（周期的参照）
                float3[] d = new float3[k + 1];
                for (int j = 0; j <= k; j++)
                {
                    int idx = ((span - k + j) % originalN + originalN) % originalN;
                    d[j] = pts[idx];
                }
                
                // De Boor's algorithm
                for (int r = 1; r <= k; r++)
                {
                    for (int j = k; j >= r; j--)
                    {
                        int left = span - k + j;
                        int right = span + 1 + j - r;
                        
                        float denominator = knots[right] - knots[left];
                        float alpha = (t - knots[left]) / denominator;
                        d[j] = math.lerp(d[j - 1], d[j], alpha);
                    }
                }
                return d[k];
            }
            else
            {
                // オープンの場合：クランプドBスプライン
                // ノットベクトルをクランプド形式で生成
                float[] knots = new float[n + k + 1];
                
                // 最初のk+1個のノットを0に設定（クランプ）
                for (int i = 0; i <= k; i++) knots[i] = 0;
                
                // 中間のノットを均等に配置
                for (int i = k + 1; i < n; i++)
                {
                    knots[i] = (float)(i - k) / (n - k);
                }
                
                // 最後のk+1個のノットを1に設定（クランプ）
                for (int i = n; i < n + k + 1; i++) knots[i] = 1;
                
                // tをノット範囲にマッピング
                t = t / (n - 1);
                t = math.clamp(t, 0f, 1f);
                
                // ノットスパンを見つける
                int span = k;
                for (int i = k + 1; i < n; i++)
                {
                    if (t < knots[i]) break;
                    span = i;
                }
                
                float3[] d = new float3[k + 1];
                for (int j = 0; j <= k; j++)
                {
                    int idx = math.clamp(span - k + j, 0, n - 1);
                    d[j] = pts[idx];
                }
                
                for (int r = 1; r <= k; r++)
                {
                    for (int j = k; j >= r; j--)
                    {
                        int left = span - k + j;
                        int right = span + 1 + j - r;
                        
                        if (left < 0 || right >= knots.Length) continue;
                        
                        float denominator = knots[right] - knots[left];
                        float alpha = denominator > 0 ? (t - knots[left]) / denominator : 0;
                        alpha = math.clamp(alpha, 0f, 1f);
                        
                        d[j] = math.lerp(d[j - 1], d[j], alpha);
                    }
                }
                return d[k];
            }
        }
        
        /// <summary>
        /// スプライン上で各制御点に最も近い位置の弧長パラメータを計算する
        /// 再帰とバックトラックを使用して、制御点の順序制約を満たす最適解を探索する
        /// </summary>
        /// <param name="spline">対象のスプライン</param>
        /// <param name="controlPoints">制御点リスト</param>
        /// <param name="closed">ループするかどうか</param>
        /// <returns>各制御点の弧長パラメータ（0～1）</returns>
        private static float[] CalculateControlPointParameters(Spline spline, IList<float3> controlPoints, bool closed)
        {
            float[] result = new float[controlPoints.Count];
            int searchSamples = spline.Count * 100;
            
            if (closed)
            {
                // クローズドの場合は順序制約なしで単純に最近点を探索
                for (int i = 0; i < controlPoints.Count; i++)
                {
                    result[i] = FindClosestPointOnSpline(spline, controlPoints[i], 0f, 1f, searchSamples);
                }
            }
            else
            {
                // オープンの場合は再帰的バックトラック探索
                if (FindParametersRecursive(spline, controlPoints, 0, 0f, result, searchSamples))
                {
                    return result;
                }
                else
                {
                    // バックトラックで解が見つからない場合のフォールバック
                    // より緩い制約で線形分割
                    for (int i = 0; i < controlPoints.Count; i++)
                    {
                        result[i] = (float)i / (controlPoints.Count - 1);
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 再帰的バックトラックによる制御点パラメータ探索
        /// </summary>
        /// <param name="spline">対象のスプライン</param>
        /// <param name="controlPoints">制御点リスト</param>
        /// <param name="currentIndex">現在処理中の制御点インデックス</param>
        /// <param name="minT">このインデックス以降で使用可能な最小のt値</param>
        /// <param name="parameters">結果を格納する配列</param>
        /// <param name="searchSamples">探索サンプル数</param>
        /// <returns>解が見つかった場合true</returns>
        private static bool FindParametersRecursive(Spline spline, IList<float3> controlPoints, int currentIndex, float minT, float[] parameters, int searchSamples)
        {
            // ベースケース：すべての制御点を処理完了
            if (currentIndex >= controlPoints.Count)
            {
                return true;
            }
            
            float3 targetPoint = controlPoints[currentIndex];
            
            // 現在の制御点に対する候補点を収集
            var candidates = FindCandidatePoints(spline, targetPoint, minT, 1f, searchSamples);
            
            // 各候補を試す
            foreach (var candidate in candidates)
            {
                parameters[currentIndex] = candidate.t;
                
                // 次の制御点の最小t値を計算（微小なマージンを追加）
                float nextMinT = candidate.t + 0.001f;
                
                // 再帰的に次の制御点を処理
                if (FindParametersRecursive(spline, controlPoints, currentIndex + 1, nextMinT, parameters, searchSamples))
                {
                    return true; // 解が見つかった
                }
                
                // バックトラック：この候補では解が見つからなかった
            }
            
            return false; // この分岐では解が見つからない
        }
        
        /// <summary>
        /// 指定範囲内で制御点に近い候補点を収集する
        /// </summary>
        /// <param name="spline">対象のスプライン</param>
        /// <param name="targetPoint">目標となる制御点</param>
        /// <param name="startT">探索開始t値</param>
        /// <param name="endT">探索終了t値</param>
        /// <param name="searchSamples">探索サンプル数</param>
        /// <returns>距離順にソートされた候補点リスト</returns>
        private static List<(float t, float distance)> FindCandidatePoints(Spline spline, float3 targetPoint, float startT, float endT, int searchSamples)
        {
            var candidates = new List<(float t, float distance)>();
            
            if (startT >= endT) return candidates; // 無効な範囲
            
            int samples = (int)(searchSamples * (endT - startT));
            samples = math.max(samples, 50); // 最低限のサンプル数を保証
            
            // 粗い探索で候補を収集
            for (int j = 0; j <= samples; j++)
            {
                float t = math.lerp(startT, endT, (float)j / samples);
                float3 splinePoint = spline.EvaluatePosition(t);
                float distance = math.distance(splinePoint, targetPoint);
                
                candidates.Add((t, distance));
            }
            
            // 距離でソート
            candidates.Sort((a, b) => a.distance.CompareTo(b.distance));
            
            // 上位の候補のみを精密化して返す（計算量を抑える）
            int maxCandidates = math.min(10, candidates.Count); // 最大10個の候補
            var refinedCandidates = new List<(float t, float distance)>();
            
            for (int i = 0; i < maxCandidates; i++)
            {
                float refinedT = RefinePosition(spline, targetPoint, candidates[i].t, searchSamples);
                float3 refinedPoint = spline.EvaluatePosition(refinedT);
                float refinedDistance = math.distance(refinedPoint, targetPoint);
                
                refinedCandidates.Add((refinedT, refinedDistance));
            }
            
            // 精密化後も距離でソート
            refinedCandidates.Sort((a, b) => a.distance.CompareTo(b.distance));
            
            return refinedCandidates;
        }
        
        /// <summary>
        /// 指定範囲内でスプライン上の最も近い点を探索する
        /// </summary>
        private static float FindClosestPointOnSpline(Spline spline, float3 targetPoint, float startT, float endT, int searchSamples)
        {
            float bestT = startT;
            float minDistance = float.MaxValue;
            
            int samples = (int)(searchSamples * (endT - startT));
            samples = math.max(samples, 100); // 最低限のサンプル数を保証
            
            for (int j = 0; j <= samples; j++)
            {
                float t = math.lerp(startT, endT, (float)j / samples);
                float3 splinePoint = spline.EvaluatePosition(t);
                float distance = math.distancesq(splinePoint, targetPoint);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestT = t;
                }
            }
            
            return RefinePosition(spline, targetPoint, bestT, searchSamples);
        }
        
        /// <summary>
        /// 指定位置周辺をより精密に探索してパラメータを改良する
        /// </summary>
        private static float RefinePosition(Spline spline, float3 targetPoint, float initialT, int searchSamples)
        {
            float bestT = initialT;
            float minDistance = float.MaxValue;
            
            // 初期位置での距離を計算
            float3 initialPoint = spline.EvaluatePosition(initialT);
            minDistance = math.distancesq(initialPoint, targetPoint);
            
            // より精密な探索：最適解周辺を細かく探索
            float searchRange = 2f / searchSamples; // 初期探索の2倍の範囲
            float startT = math.max(0f, initialT - searchRange);
            float endT = math.min(1f, initialT + searchRange);
            
            int refinementSamples = 200; // より高精度
            for (int j = 0; j <= refinementSamples; j++)
            {
                float t = math.lerp(startT, endT, (float)j / refinementSamples);
                float3 splinePoint = spline.EvaluatePosition(t);
                float distance = math.distancesq(splinePoint, targetPoint);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestT = t;
                }
            }
            
            return bestT;
        }
        
        /// <summary>
        /// 制御点パラメータから両端を除いた範囲を取得する（SplineExtrude用）
        /// </summary>
        /// <param name="controlPointParameters">制御点の弧長パラメータ配列</param>
        /// <returns>開始と終了の弧長パラメータのタプル</returns>
        public static (float start, float end) GetExtrudeRange(float[] controlPointParameters)
        {
            if (controlPointParameters == null || controlPointParameters.Length < 2)
                throw new ArgumentException("制御点パラメータは2個以上必要です");
                
            // 両端を除いた範囲を返す
            float start = controlPointParameters[1]; // 最初の制御点を除く
            float end = controlPointParameters[controlPointParameters.Length - 2]; // 最後の制御点を除く
            
            return (start, end);
        }
        
        /// <summary>
        /// 制御点パラメータから指定した範囲を取得する
        /// </summary>
        /// <param name="controlPointParameters">制御点の弧長パラメータ配列</param>
        /// <param name="startIndex">開始制御点のインデックス</param>
        /// <param name="endIndex">終了制御点のインデックス</param>
        /// <returns>指定範囲の開始と終了の弧長パラメータのタプル</returns>
        public static (float start, float end) GetExtrudeRange(float[] controlPointParameters, int startIndex, int endIndex)
        {
            if (controlPointParameters == null)
                throw new ArgumentNullException(nameof(controlPointParameters));
            if (startIndex < 0 || startIndex >= controlPointParameters.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (endIndex < 0 || endIndex >= controlPointParameters.Length)
                throw new ArgumentOutOfRangeException(nameof(endIndex));
            if (startIndex >= endIndex)
                throw new ArgumentException("開始インデックスは終了インデックスより小さい必要があります");
                
            return (controlPointParameters[startIndex], controlPointParameters[endIndex]);
        }
        
        /// <summary>
        /// 制御点とスプライン上の対応点の距離を計算する（デバッグ用）
        /// </summary>
        /// <param name="spline">対象のスプライン</param>
        /// <param name="controlPoints">制御点リスト</param>
        /// <param name="controlPointParameters">制御点パラメータ</param>
        /// <returns>各制御点とスプライン上の対応点の距離配列</returns>
        public static float[] GetControlPointDistances(Spline spline, IList<float3> controlPoints, float[] controlPointParameters)
        {
            if (spline == null) throw new ArgumentNullException(nameof(spline));
            if (controlPoints == null) throw new ArgumentNullException(nameof(controlPoints));
            if (controlPointParameters == null) throw new ArgumentNullException(nameof(controlPointParameters));
            if (controlPoints.Count != controlPointParameters.Length)
                throw new ArgumentException("制御点とパラメータの数が一致しません");
                
            float[] distances = new float[controlPoints.Count];
            
            for (int i = 0; i < controlPoints.Count; i++)
            {
                float3 splinePoint = spline.EvaluatePosition(controlPointParameters[i]);
                distances[i] = math.distance(controlPoints[i], splinePoint);
            }
            
            return distances;
        }
        
        /// <summary>
        /// 制御点パラメータが適切な順序になっているかを検証する
        /// </summary>
        /// <param name="controlPointParameters">制御点パラメータ配列</param>
        /// <param name="closed">ループするかどうか</param>
        /// <returns>順序が正しい場合true</returns>
        public static bool ValidateParameterOrder(float[] controlPointParameters, bool closed)
        {
            if (controlPointParameters == null || controlPointParameters.Length < 2)
                return true;
                
            if (closed) return true; // クローズドの場合は順序の概念が異なる
            
            for (int i = 1; i < controlPointParameters.Length; i++)
            {
                if (controlPointParameters[i] <= controlPointParameters[i - 1])
                {
                    return false;
                }
            }
            
            return true;
        }
    }
}