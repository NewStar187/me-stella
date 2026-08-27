// Gate 0 spike — 판정 후 폐기 가능
// F항목: 웹 셸이 서빙하는 수식 이미지(/formula-sample.png)를 런타임에 내려받아
//        Unity 씬의 RawImage에 원본 비율로 표시한다.

using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MeStella.Spike
{
    /// <summary>
    /// 웹 셸의 정적 이미지 1장을 UnityWebRequestTexture로 로드해 RawImage에 붙인다.
    /// </summary>
    public class FormulaImageLoader : MonoBehaviour
    {
        [Header("로드 설정")]
        [Tooltip("origin 뒤에 붙일 이미지 경로.")]
        [SerializeField] private string imagePath = "/formula-sample.png";

        [Tooltip("에디터 실행 등 absoluteURL이 비어 있을 때 사용할 폴백 origin.")]
        [SerializeField] private string editorFallbackOrigin = "https://me-stella.vercel.app";

        [Tooltip("요청 타임아웃(초).")]
        [SerializeField] private int requestTimeoutSeconds = 15;

        [Header("표시")]
        [Tooltip("이미지를 표시할 RawImage.")]
        [SerializeField] private RawImage targetImage;

        [Tooltip("상태·에러 메시지를 표시할 UI Text(레거시).")]
        [SerializeField] private Text statusText;

        private IEnumerator Start()
        {
            if (targetImage == null)
            {
                const string message = "RawImage가 연결되지 않았다.";
                Debug.LogError("[Spike/F] " + message);
                SetStatus("실패 — " + message);
                yield break;
            }

            string url = ResolveOrigin() + imagePath;
            SetStatus("로드 중 — " + url);
            Debug.Log("[Spike/F] 이미지 로드 시작 — " + url);

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                request.timeout = requestTimeoutSeconds;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string message = $"실패 — {request.result}, HTTP {request.responseCode}, {request.error}\n{url}";
                    Debug.LogError("[Spike/F] " + message);
                    SetStatus(message);
                    yield break;
                }

                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                if (texture == null || texture.width <= 0 || texture.height <= 0)
                {
                    string message = "실패 — 응답을 텍스처로 해석하지 못했다.\n" + url;
                    Debug.LogError("[Spike/F] " + message);
                    SetStatus(message);
                    yield break;
                }

                targetImage.texture = texture;
                targetImage.color = Color.white;
                ApplyAspectRatio(texture);

                string ok = $"성공 — {texture.width}x{texture.height}px";
                Debug.Log($"[Spike/F] {ok} ({url})");
                SetStatus(ok);
            }
        }

        /// <summary>
        /// 원본 비율 유지를 위해 RawImage에 AspectRatioFitter(FitInParent)를 붙이고 비율을 맞춘다.
        /// 씬 파일을 건드리지 않기 위해 런타임에 컴포넌트를 확보한다.
        /// </summary>
        private void ApplyAspectRatio(Texture2D texture)
        {
            AspectRatioFitter fitter = targetImage.GetComponent<AspectRatioFitter>();
            if (fitter == null)
            {
                fitter = targetImage.gameObject.AddComponent<AspectRatioFitter>();
            }

            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = (float)texture.width / texture.height;

            // uvRect가 변형돼 있으면 원본 비율이 어긋나므로 기본값으로 되돌린다.
            targetImage.uvRect = new Rect(0f, 0f, 1f, 1f);
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

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
    }
}
