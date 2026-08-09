namespace Treeline
{
    /// <summary>
    /// 장(레벨) 씬 이름과 진행 순서.
    /// </summary>
    /// <remarks>
    /// 씬을 빌드 인덱스가 아니라 <b>이름으로</b> 부르기 때문에 문자열이 흩어지면
    /// 오타가 런타임까지 간다. 그래서 한 곳에 모았다.
    /// <para>
    /// 1단계는 1 · 2 · 6 만 만든다. 3 · 4 · 5 는 나중에 <see cref="Order"/> 의
    /// 가운데에 끼워 넣는다. 순서를 배열로 둔 이유가 그것이고, 장을 추가할 때
    /// 고칠 곳은 이 파일 하나다.
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
        /// <remarks>
        /// IMPORTANT: <b>빌드 목록에만 넣고 여기 빠뜨리면 그 장은 조용히 반쪽이
        /// 된다.</b> <see cref="GameFlow"/> 가 <see cref="IsChapter"/> 로 「샌드박스인가」를
        /// 가리므로, 목록에 없는 씬에서는 진행 시스템이 아예 안 뜬다 — <b>죽어도
        /// 재시작하지 않고 출구를 밟아도 다음 장으로 안 넘어간다.</b> 에러도 경고도
        /// 없어서 씬을 의심하게 된다. 2026-08-09 에 3장이 정확히 이랬다.
        ///
        /// 반대로 <b>덜 만든 씬을 여기 넣어도 안 된다.</b> 앞 장을 깬 사람이 빈
        /// 화면에 떨어진다. 새 장을 붙이는 순서는 「씬을 완성 → 빌드 목록 → 여기」다.
        /// </remarks>
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
