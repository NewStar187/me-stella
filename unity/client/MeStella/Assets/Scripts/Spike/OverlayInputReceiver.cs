// Gate 0 spike — 판정 후 폐기 가능
// D항목: HTML 오버레이 <input>의 한글 IME 입력을 JS SendMessage로 받아 그대로 표시한다.
//        Unity 내장 입력(InputField/TouchScreenKeyboard)은 사용하지 않는다(확정 방침).

using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MeStella.Spike
{
    /// <summary>
    /// 웹 템플릿의 unityInstance.SendMessage("SpikeManager", "OnOverlayInput", value) 수신부.
    /// 이 컴포넌트가 붙은 GameObject의 이름은 반드시 "SpikeManager"여야 한다.
    /// </summary>
    public class OverlayInputReceiver : MonoBehaviour
    {
        [Header("표시")]
        [Tooltip("수신 문자열을 그대로 출력할 UI Text(레거시). 한글 글리프가 있는 폰트를 지정할 것.")]
        [SerializeField] private Text targetText;

        [Tooltip("아직 입력이 없을 때 보여줄 문구.")]
        [SerializeField] private string placeholder = "(HTML 입력 대기 중)";

        [Tooltip("수신 문자열의 유니코드 코드포인트도 함께 로그로 남긴다. 폰트에 한글 글리프가 없어 화면이 비어 보일 때 조합 결과를 확인하는 용도.")]
        [SerializeField] private bool logCodePoints = true;

        /// <summary>가장 최근에 수신한 문자열.</summary>
        public string LatestValue { get; private set; } = string.Empty;

        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // 기본값(true)이면 Unity 캔버스가 브라우저 키 입력을 가로채 HTML <input>에 글자가 들어가지 않는다.
            // D항목의 전제 조건이므로 반드시 꺼 둔다.
            WebGLInput.captureAllKeyboardInput = false;
#endif
        }

        private void Start()
        {
            if (targetText != null && string.IsNullOrEmpty(LatestValue))
            {
                targetText.text = placeholder;
            }
        }

        /// <summary>JS SendMessage 수신 진입점. 파라미터 1개(string) 시그니처를 유지해야 한다.</summary>
        public void OnOverlayInput(string value)
        {
            LatestValue = value ?? string.Empty;

            if (targetText != null)
            {
                // 조합 중인 글자까지 그대로 보기 위해 가공하지 않는다.
                targetText.text = LatestValue;
            }

            if (logCodePoints)
            {
                Debug.Log($"[Spike/D] len={LatestValue.Length} value=\"{LatestValue}\" cp=[{DescribeCodePoints(LatestValue)}]");
            }
            else
            {
                Debug.Log($"[Spike/D] len={LatestValue.Length} value=\"{LatestValue}\"");
            }
        }

        /// <summary>문자열을 U+XXXX 목록으로 풀어 쓴다(서로게이트 쌍 포함).</summary>
        private static string DescribeCodePoints(string value)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < value.Length; i++)
            {
                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                // 짝이 맞는 서로게이트 쌍만 합쳐서 계산하고, 짝이 없으면 그 값 그대로 표기한다(예외 방지).
                int codePoint;
                if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    codePoint = char.ConvertToUtf32(value[i], value[i + 1]);
                    i++;
                }
                else
                {
                    codePoint = value[i];
                }

                builder.Append("U+").Append(codePoint.ToString("X4"));
            }

            return builder.ToString();
        }
    }
}
