namespace Treeline
{
    /// <summary>
    /// 장(레벨) 씬 이름과 진행 순서.
    /// </summary>
    /// <remarks>
    /// 씬을 빌드 인덱스가 아니라 <b>이름으로</b> 부르기 때문에 문자열이 흩어지면
    /// 오타가 런타임까지 간다. 그래서 한 곳에 모았다.
    /// <para>
    /// 1단계는 1 · 2 · 6 만 만들었고, 2026-08-09 에 <b>3장과 4장을 가운데에
    /// 끼웠다.</b> 순서를 배열로 둔 이유가 그것이고, 장을 추가할 때 고칠 곳은 이
    /// 파일 하나다. 남은 것은 5장이며 <b>컷라인 1순위</b>라 없어도 완결이다.
    /// </para>
    /// <para>
    /// IMPORTANT: <b>여기 넣기 전에 그 장이 실제로 걸어져야 한다.</b> 짓는 중인
    /// 장을 먼저 끼우면 앞 장의 출구가 미완성 장으로 이어져 관통이 끊긴다.
    /// 빌드 씬 목록에만 올려 두면 이름으로 열어 확인하면서 관통은 그대로 둘 수 있다.
    /// </para>
    /// </remarks>
    public static class Chapters
    {
        public const string Ch1 = "Ch1";
        public const string Ch2 = "Ch2";
        public const string Ch3 = "Ch3";
        public const string Ch4 = "Ch4";
        public const string Ch5 = "Ch5";
        public const string Ch6 = "Ch6";

        /// <summary>게임이 시작되는 곳. 빌드 씬 목록의 맨 앞이다.</summary>
        public const string Title = "Title";

        /// <summary>마지막 장을 빠져나온 뒤 가는 곳.</summary>
        public const string Ending = "Ending";

        /// <summary>
        /// 실제로 진행하는 순서. <b>여기 있는 것만 게임에 들어간다.</b>
        /// 빌드 씬 목록과 내용이 같아야 한다.
        /// </summary>
        public static readonly string[] Order =
        {
            Ch1,
            Ch2,
            Ch3,
            Ch4,
            Ch6,
        };

        /// <summary>첫 장.</summary>
        public static string First => Order[0];

        /// <summary>
        /// 다음 장 이름. 마지막 장이면 <see cref="Ending"/> 을 준다.
        /// 목록에 없는 이름이면 첫 장으로 되돌린다(샌드박스에서 실행한 경우).
        /// </summary>
        public static string Next(string current)
        {
            int index = System.Array.IndexOf(Order, current);
            if (index < 0)
            {
                return First;
            }

            return index + 1 < Order.Length ? Order[index + 1] : Ending;
        }

        /// <summary>진행 순서에 들어 있는 장인지. 샌드박스 씬과 구분할 때 쓴다.</summary>
        public static bool IsChapter(string sceneName)
        {
            return System.Array.IndexOf(Order, sceneName) >= 0;
        }
    }
}
