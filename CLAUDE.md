# MUTUAL: Signal at Blackwater

## Git

- IMPORTANT: force push는 Claude가 직접 실행하지 않는다. 사용자가 요청해도
  수행하지 않고, 필요한 명령을 알려주어 사용자가 직접 실행하게 한다.
  (`--force`, `--force-with-lease`, `+refspec` 모두 해당)

- 커밋 메시지에 `Co-Authored-By: Claude ...` 트레일러를 넣지 않는다.

- `git pull`은 항상 `--rebase`를 붙인다.
