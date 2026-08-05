---
description: Unity WebGL 빌드 산출물을 gh-pages 브랜치에 올려 GitHub Pages 로 배포한다. 빌드는 사람이 Unity 에디터에서 하고, 이 스킬은 그 결과를 배포한다. 배포하거나 배포 상태를 확인할 때 사용한다.
---

# WebGL 배포하기

배포 주소는 `https://dev-kkyu.github.io/mutual-game/` 다. 소스는 `develop`,
서빙되는 파일은 `gh-pages` 브랜치에 있다.

## 배포 전에 확인할 것

Unity 빌드는 사람이 에디터에서 한다. 아래 두 가지가 맞는지 먼저 확인하고,
아니면 사용자에게 알린다.

- **출력 폴더가 `Mutual/Build/`** 인가. `.gitignore` 가 `Build/` 를 제외하므로
  소스 브랜치가 더러워지지 않는다.
- **Decompression Fallback 이 켜져 있는가.**
  `Mutual/ProjectSettings/ProjectSettings.asset` 의 `webGLDecompressionFallback`
  이 `1` 이어야 한다. `0` 이면 GitHub Pages 에서 로딩이 멈춘다. 이유는
  `docs/tech-design/decisions.md` 참고.

빌드가 없으면 사용자에게 Unity 에서 빌드해 달라고 요청한다. 대신 만들 수 없다.

## 절차

작업 트리를 건드리지 않도록 **별도 worktree** 에서 진행한다. `develop` 에서
작업 중인 내용이 있어도 안전하다.

```bash
wt="<스크래치패드>/ghpages"
git worktree add "$wt" gh-pages
```

빌드 산출물을 브랜치 루트로 복사한다. `Mutual/Build/` 자체가 아니라 **그 안의
내용**을 옮긴다. `index.html` 이 루트에 있어야 한다.

```bash
rm -rf "$wt"/Build "$wt"/TemplateData "$wt"/index.html
cp -r Mutual/Build/. "$wt/"
```

`.nojekyll` 과 `.gitattributes` 는 브랜치에 이미 있으므로 지우지 않는다. 위
`rm` 이 그 둘을 건드리지 않는지 확인한다.

커밋하고 정리한다.

```bash
cd "$wt" && git add -A && git commit
cd - && git worktree remove --force "$wt"
```

IMPORTANT: **push 는 별도 지시를 받고 한다.** 커밋과 push 는 별개 작업이다.

## 배포 후

`https://dev-kkyu.github.io/mutual-game/` 가 갱신되기까지 1~2분 걸린다.
`WebFetch` 로 `<title>` 이 나오는지 확인할 수 있지만, **JS 를 실행하지 않으므로
게임이 실제로 뜨는지는 알 수 없다.** 그건 사용자가 브라우저에서 확인해야 한다.

## 자주 배포하지 않는다

배포마다 산출물 전체가 브랜치에 쌓인다. 게임이 커지면 한 번에 수십 MB 다.

**빌드 확인은 로컬 웹서버로 한다.** 실제 배포와 거의 같은 조건이고 저장소가
무거워지지 않는다.

```bash
cd Mutual/Build && python -m http.server 8000
```

`8080` 은 Unity MCP 브리지가 쓰므로 피한다.

배포는 **공유하거나 제출 상태를 확인할 때만** 한다.

## 브랜치가 무거워지면

`gh-pages` 는 빌드 산출물만 있어 과거 이력을 보존할 이유가 없다. 무거워지면
orphan 으로 다시 만들어 히스토리를 비운다.

IMPORTANT: 그 작업은 force push 가 필요하므로 **Claude 가 실행하지 않는다.**
필요한 명령을 사용자에게 알려주고 직접 실행하게 한다.
