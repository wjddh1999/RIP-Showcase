# Player Services

계정 인증을 프로필·로드아웃 저장과 연결하는 개인 구현 중심의 UGS 클라이언트 코드입니다.

## Files

| 파일 | 역할 |
|---|---|
| [PlayerProfile.cs](./PlayerProfile.cs) | Player ID·닉네임·ELO 프로필 |
| [UgsPlayerService.cs](./UgsPlayerService.cs) | 로그인 완료·수명주기 확인·Cloud Save·점수 조회 관련 멤버 발췌 |

## Authentication Lifecycle

Unity Player Accounts에서 받은 액세스 토큰으로 UGS Authentication에 로그인합니다. 비동기 응답 적용 전에 lifecycle epoch와 현재 Player ID를 확인합니다. 프로필·로드아웃을 읽은 뒤에도 같은 계정인지 확인해 로그인 상태를 반영합니다.

## Loadout Persistence

Cloud Save에서 로드아웃을 읽고 데이터 버전을 확인합니다. 초기 데이터가 필요하면 기본 로드아웃을 생성·저장합니다. 로컬 변경의 저장 요청은 SemaphoreSlim으로 직렬화하고 저장 직전에 로그인·게스트·Holder 상태를 다시 확인합니다. 게스트는 이 저장 경로에서 제외합니다.

## Leaderboard Access

ELO를 조회하고 최초 항목이 없으면 기본 점수를 등록합니다. 랭킹 범위는 조회·초기 등록이며 경기 결과 정산은 포함하지 않습니다.

## Dependencies

UGS Authentication·Unity Player Accounts·Cloud Save·Leaderboards와 로드아웃 타입이 필요합니다. 서비스 초기화·프로필 로더·로그아웃·이벤트 구독 등 다른 멤버와 서비스 설정은 포함하지 않아 단독 실행되지 않습니다.
