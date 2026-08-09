using TMPro;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 메뉴 화면 셋(타이틀 · 일시정지 · 엔딩)이 함께 쓰는 그림 · 글꼴 · 소리.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>왜 에셋 하나로 묶었나.</b> 일시정지 화면은 씬에 놓이지 않고 코드로 생기므로
    /// 인스펙터에 참조를 끼울 자리가 없다. 그렇다고 그림마다
    /// <see cref="Resources"/> 로 부르게 하면, 그림을 담당 폴더에서 꺼내
    /// <c>Resources/</c> 로 옮겨야 한다 — 소리는 <c>Audio/</c> 에 카테고리로 두는
    /// 규칙이 있어서 그럴 수 없다.
    /// </para>
    /// <para>
    /// 이 에셋만 <c>Resources/</c> 에 두면 참조된 그림과 소리는 <b>있던 자리에
    /// 그대로 두고도</b> 빌드에 들어간다. 부르는 쪽은 한 줄이다.
    /// </para>
    /// <para>
    /// IMPORTANT: 경로가 <c>Assets/Game/UI/Resources/Menu/MenuTheme.asset</c> 로
    /// 박혀 있다. 옮기면 세 화면이 한꺼번에 민무늬가 된다.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(menuName = "Treeline/Menu Theme", fileName = "MenuTheme")]
    public sealed class MenuTheme : ScriptableObject
    {
        public const string ResourcePath = "Menu/MenuTheme";

        [Header("Frames")]
        [SerializeField] private Sprite panelFrame;
        [SerializeField] private Sprite buttonFrame;
        [SerializeField] private Sprite divider;

        [Header("Typography")]
        [SerializeField] private TMP_FontAsset regularFont;
        [SerializeField] private TMP_FontAsset semiBoldFont;

        [Header("Sound")]
        [Tooltip("버튼을 고르고 누를 때. 고를 때는 더 높고 작게 낸다.")]
        [SerializeField] private AudioClip clickClip;

        public Sprite PanelFrame => panelFrame;
        public Sprite ButtonFrame => buttonFrame;
        public Sprite Divider => divider;
        public TMP_FontAsset RegularFont => regularFont;
        public TMP_FontAsset SemiBoldFont => semiBoldFont;
        public AudioClip ClickClip => clickClip;

        public static MenuTheme Load()
        {
            var theme = Resources.Load<MenuTheme>(ResourcePath);
            if (theme == null)
            {
                Debug.LogError($"[{nameof(MenuTheme)}] 'Resources/{ResourcePath}' 이 없다. " +
                               "메뉴가 테두리도 글꼴도 없이 뜬다.");
            }
            return theme;
        }
    }
}
