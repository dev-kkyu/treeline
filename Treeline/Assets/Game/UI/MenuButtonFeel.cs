using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 메뉴 버튼이 마우스와 키보드에 반응하게 한다. 왼쪽 표시선이 자라고 글자가
    /// 안쪽으로 밀리며 짧은 소리가 난다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Button"/> 의 <c>ColorTint</c> 는 색만 바꾼다. 어두운 화면에서
    /// 색 변화는 거의 안 보이므로 <b>움직이는 것</b>을 하나 둔다 — 눈은 밝기보다
    /// 움직임을 먼저 잡는다.
    /// </para>
    /// <para>
    /// 마우스와 키보드를 <b>따로 세지 않는다.</b> 포인터가 올라간 것과 선택된 것을
    /// 각각 상태로 두면 마우스로 A 에 올린 채 키보드로 B 를 고르는 순간 둘 다
    /// 켜진다. 매 프레임 「지금 초점이 있나」 하나로 판정한다.
    /// </para>
    /// <para>
    /// 소리는 <b>초점이 바뀌는 순간에만</b> 낸다. 마우스가 버튼 위에서 움직이는
    /// 동안 계속 나면 지직거린다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MenuButtonFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float RestMarkWidth = 3f;
        private const float FocusMarkWidth = 7f;
        private const float LabelSlide = 12f;

        private Button button;
        private Image mark;
        private RectTransform markRect;
        private TMP_Text label;
        private float labelRestLeft;
        private Color markColor;

        private AudioSource speaker;
        private AudioClip focusClip;

        private float focus;
        private bool pointerOver;
        private bool hadFocus;

        public void Bind(Button owner, Image focusMark, TMP_Text text, float restLeft,
            AudioSource audio, AudioClip clip)
        {
            button = owner;
            mark = focusMark;
            markRect = focusMark.rectTransform;
            markColor = focusMark.color;
            label = text;
            labelRestLeft = restLeft;
            speaker = audio;
            focusClip = clip;
            Redraw(0f);
        }

        private void OnDisable()
        {
            // 페이지가 닫혔다가 다시 열릴 때 눌린 채로 남아 있으면 안 된다.
            pointerOver = false;
            hadFocus = false;
            focus = 0f;
            if (mark != null)
            {
                Redraw(0f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerOver = true;

            // 마우스를 올린 것도 「고른 것」으로 친다. 안 그러면 마우스로 훑다가
            // 엔터를 치면 엉뚱한 버튼이 눌린다.
            if (button != null && button.interactable && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerOver = false;
        }

        private void Update()
        {
            bool focused = button != null && button.interactable
                && (pointerOver || (EventSystem.current != null
                    && EventSystem.current.currentSelectedGameObject == button.gameObject));

            if (focused && !hadFocus && speaker != null && focusClip != null)
            {
                // 같은 클립을 눌렀을 때보다 높고 작게 낸다. 클립을 하나만 만들었다.
                speaker.pitch = 1.45f;
                speaker.PlayOneShot(focusClip, 0.22f);
                speaker.pitch = 1f;
            }
            hadFocus = focused;

            focus = Mathf.MoveTowards(focus, focused ? 1f : 0f, Time.unscaledDeltaTime * 7f);
            Redraw(focus * focus * (3f - 2f * focus));
        }

        private void Redraw(float eased)
        {
            markRect.sizeDelta = new Vector2(Mathf.Lerp(RestMarkWidth, FocusMarkWidth, eased),
                markRect.sizeDelta.y);
            mark.color = new Color(markColor.r, markColor.g, markColor.b,
                Mathf.Lerp(0.3f, 1f, eased));

            if (label != null)
            {
                Vector2 offset = label.rectTransform.offsetMin;
                label.rectTransform.offsetMin = new Vector2(labelRestLeft + eased * LabelSlide, offset.y);
                label.characterSpacing = Mathf.Lerp(0f, 3f, eased);
            }
        }
    }
}
