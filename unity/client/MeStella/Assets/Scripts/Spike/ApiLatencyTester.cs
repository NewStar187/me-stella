// Gate 0 spike — 판정 후 폐기 가능
// E항목: Unity(WebGL) → 웹 셸 /api/health 왕복 지연을 20회 순차 측정하고
//        최소 / 중앙값 / P75 / 최대를 산출한다. 판정·게임 로직 아님(측정 전용).

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace MeStella.Spike
{
    /// <summary>
    /// /api/health 왕복 지연 측정기. RunTest()를 버튼 등에서 호출한다.
    /// </summary>
    public class ApiLatencyTester : MonoBehaviour
    {
        [Header("측정 설정")]
        [Tooltip("측정 횟수. Gate 0 기준은 20회.")]
        [SerializeField] private int sampleCount = 20;

        [Tooltip("origin 뒤에 붙일 API 경로.")]
        [SerializeField] private string apiPath = "/api/health";

        [Tooltip("에디터 실행 등 absoluteURL이 비어 있을 때 사용할 폴백 origin.")]
        [SerializeField] private string editorFallbackOrigin = "https://me-stella.vercel.app";

        [Tooltip("요청당 타임아웃(초).")]
        [SerializeField] private int requestTimeoutSeconds = 15;

        [Tooltip("CDN·브라우저 캐시가 왕복 시간을 왜곡하지 않도록 쿼리 파라미터를 덧붙인다.")]
        [SerializeField] private bool appendCacheBuster = true;

        [Tooltip("씬 재생과 동시에 자동 측정 시작(버튼 배선 없이 확인할 때).")]
        [SerializeField] private bool runOnStart = false;

        [Header("표시")]
        [Tooltip("진행 상황과 결과를 출력할 UI Text(레거시).")]
        [SerializeField] private Text resultText;

        private bool isRunning;
        private readonly List<double> samples = new List<double>();

        private void Start()
        {
            Report("대기 중 — RunTest() 호출 시 측정을 시작한다.");
            if (runOnStart)
            {
                RunTest();
            }
        }

        /// <summary>측정 시작 진입점. 버튼 OnClick 또는 외부 스크립트에서 호출한다.</summary>
        public void RunTest()
        {
            if (isRunning)
            {
                Debug.LogWarning("[Spike/E] 이미 측정이 진행 중이다. 중복 실행을 무시한다.");
                return;
            }

            StartCoroutine(RunTestRoutine());
        }

        private IEnumerator RunTestRoutine()
        {
            isRunning = true;
            samples.Clear();

            int failureCount = 0;
            string origin = ResolveOrigin();
            Debug.Log($"[Spike/E] 측정 시작 — origin={origin}, path={apiPath}, n={sampleCount}");
            Report($"origin: {origin}\n0/{sampleCount} ...");

            for (int i = 1; i <= sampleCount; i++)
            {
                string url = BuildUrl(origin, i);

                // using 블록 안에서 yield 하므로 요청은 항상 1건씩 순차 실행된다(동시 호출 없음).
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.timeout = requestTimeoutSeconds;

                    Stopwatch stopwatch = Stopwatch.StartNew();
                    yield return request.SendWebRequest();
                    stopwatch.Stop();

                    double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        samples.Add(elapsedMs);
                        Debug.Log($"[Spike/E] {i}/{sampleCount} {elapsedMs:F1} ms (HTTP {request.responseCode})");
                    }
                    else
                    {
                        failureCount++;
                        Debug.LogWarning(
                            $"[Spike/E] {i}/{sampleCount} 실패 — {request.result}, HTTP {request.responseCode}, {request.error}");
                    }
                }

                Report($"origin: {origin}\n{i}/{sampleCount} ...");

                // 한 프레임 양보해 브라우저가 연결을 정리할 여유를 준다.
                yield return null;
            }

            Report(BuildSummary(origin, failureCount));
            isRunning = false;
        }

        private string BuildUrl(string origin, int index)
        {
            string url = origin + apiPath;
            if (!appendCacheBuster)
            {
                return url;
            }

            string separator = url.Contains("?") ? "&" : "?";
            // 프레임 카운터를 섞어 같은 회차라도 URL이 겹치지 않게 한다.
            return $"{url}{separator}spike={index}-{Time.frameCount}";
        }

        /// <summary>
        /// 브라우저에서 실행 중이면 Application.absoluteURL에서 scheme+host+port를 뽑고,
        /// 에디터 등에서 비어 있으면 폴백 origin을 쓴다.
        /// </summary>
        private string ResolveOrigin()
        {
            string absoluteUrl = Application.absoluteURL;

            if (!string.IsNullOrEmpty(absoluteUrl)
                && System.Uri.TryCreate(absoluteUrl, System.UriKind.Absolute, out System.Uri uri)
                && (uri.Scheme == System.Uri.UriSchemeHttp || uri.Scheme == System.Uri.UriSchemeHttps))
            {
                return uri.GetLeftPart(System.UriPartial.Authority);
            }

            return (editorFallbackOrigin ?? string.Empty).TrimEnd('/');
        }

        private string BuildSummary(string origin, int failureCount)
        {
            if (samples.Count == 0)
            {
                string failMessage = $"origin: {origin}\n측정 실패 — 성공 응답 0건 / 실패 {failureCount}건";
                Debug.LogError("[Spike/E] " + failMessage);
                return failMessage;
            }

            List<double> sorted = new List<double>(samples);
            sorted.Sort();

            double min = sorted[0];
            double max = sorted[sorted.Count - 1];
            double median = Median(sorted);
            double p75 = NearestRankPercentile(sorted, 0.75d);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"origin: {origin}");
            builder.AppendLine($"성공 {sorted.Count} / 실패 {failureCount} (요청 {sampleCount}회)");
            builder.AppendLine($"최소   {min:F1} ms");
            builder.AppendLine($"중앙값 {median:F1} ms");
            builder.AppendLine($"P75    {p75:F1} ms");
            builder.AppendLine($"최대   {max:F1} ms");
            string summary = builder.ToString().TrimEnd();

            Debug.Log("[Spike/E] 측정 완료\n" + summary);
            Debug.Log("[Spike/E] raw(ms, 측정 순서): " + JoinSamples(samples));

            return summary;
        }

        private static string JoinSamples(List<double> values)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(values[i].ToString("F1", CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        /// <summary>짝수 개면 가운데 두 값의 평균, 홀수 개면 가운데 값.</summary>
        private static double Median(List<double> sorted)
        {
            int count = sorted.Count;
            if (count == 0)
            {
                return 0d;
            }

            if (count % 2 == 1)
            {
                return sorted[count / 2];
            }

            return (sorted[(count / 2) - 1] + sorted[count / 2]) * 0.5d;
        }

        /// <summary>최근접 순위(nearest-rank) 백분위. n=20, p=0.75이면 정렬 후 15번째 값.</summary>
        private static double NearestRankPercentile(List<double> sorted, double percentile)
        {
            if (sorted.Count == 0)
            {
                return 0d;
            }

            int rank = Mathf.CeilToInt((float)(percentile * sorted.Count));
            rank = Mathf.Clamp(rank, 1, sorted.Count);
            return sorted[rank - 1];
        }

        private void Report(string message)
        {
            if (resultText != null)
            {
                resultText.text = message;
            }
        }
    }
}
