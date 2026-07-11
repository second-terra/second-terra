<div align="center">
  
# 🎮 Second-Terra

**장르:** 탑뷰 액션, SF, 로그라이트 <br>
</div>

<br>

## 📌 Second-terra는 이런 게임이에요

> 지구가 살기 어려운 환경으로 변한 미래, 주인공은 원격 조종 의체(儀體)로 외행성에 투입되어 토착 적대 생명체를 소탕하고 새로운 거주지를 개척하는 탑뷰 로그라이트 액션 게임

### 🔥 낯선 행성을 개척하는 이야기

> 소탕 인원은 전문 훈련된 전략 자산이기 때문에, 직접 투입되지 않고 원격 조종 의체를 대신 보낸다

지구가 붕괴하고 도착한 외행성엔 얼핏 푸른빛이 돌지만, 표면 스캔엔 붉은 점(적대 생명체)이 가득하다. 플레이어는 의체를 조종해 토착 생명체를 소탕하고, 행성을 사람이 살 수 있는 곳으로 바꿔나간다.

### 🔥 무기마다 다른 의체, 근접·원거리로 나뉘는 적

> 무기별로 캐릭터, 즉 의체가 하나씩 존재한다

플레이어는 무기가 다른 3종의 의체 중 하나를 골라 전투에 나선다. 맞서는 적은 생체 기술을 사용하는 근접형(일반 / 자폭 / 돌진)과 원거리형으로 나뉘며, 섹터마다 등장하는 적의 성격이 달라진다.

### 🔥 섹터마다 다른 위협, 미션을 넘어 보스전까지

> 미션 2개를 완료해야 보스전이 열리고, 보스전을 끝내면 섹터를 확보함

바람 고원(빠르고 약한 적), 바위 협곡(느리지만 단단한 적), 진흙 늪지(죽으면 독안개를 남기는 적)까지 3개 섹터를 확보하면 게임이 끝난다. 미션을 클리어할 때마다 얻는 재화로 스탯을 성장시킬 수 있다.

<br>

## 👤 second-terra의 구성원을 소개합니다!

| 역할 | 담당 |
|:---|:---|
| 📋 기획 | 박가원 |
| 💻 기능 구현 | 곽의정 · 배서윤 · 이윤지 |
| 🎨 UI · 이펙트 | 김세원 |
| 🖼️ 메인아트 | 신지은 |
| 🎨 서브아트 | 박태영 · 조혜원 · 신윤서 |
| ✨ 로고 | 제서영 |
| 🧩 서포트 | 김민호 |

<br>

## 🖥️ 기술 스택

| 역할 | 종류 | 선정 이유 |
|---|---|---|
| Engine | ![Unity](https://img.shields.io/badge/Unity-2022.3.62f3-000000?style=for-the-badge&logo=unity&logoColor=white) | 2D 탑뷰 게임 개발에 필요한 스프라이트/물리/애니메이션 기능이 안정적으로 지원되는 LTS 버전 |
| Language | ![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white) | Unity의 기본 스크립팅 언어로, 컴포넌트 기반 구조와 상속을 활용해 적/플레이어 로직을 재사용 가능하게 설계 |
| 형상 관리 | ![Git](https://img.shields.io/badge/Git-F05032?style=for-the-badge&logo=git&logoColor=white) ![GitHub](https://img.shields.io/badge/GitHub-181717?style=for-the-badge&logo=github&logoColor=white) | 브랜치 전략과 PR 기반 협업으로 여러 명이 동시에 작업해도 충돌을 최소화 |
| 코드 리뷰 | ![CodeRabbit](https://img.shields.io/badge/CodeRabbit-FF570A?style=for-the-badge&logo=coderabbit&logoColor=white) | PR마다 자동으로 리뷰를 붙여, 사람 리뷰 전에 명백한 이슈를 먼저 걸러냄 |

<br>

## ⚙️ 실행 환경

- **Unity 버전**: `2022.3.62f3`
- **여는 방법**
  git clone https://github.com/second-terra/second-terra.git
Unity Hub → Projects → Add → 클론한 폴더 선택 → 실행

<br>

## 🔗 Git Convention

### 1️⃣ Git Flow

```
  develop ← 작업 브랜치
```

- `main branch` : 배포 브랜치
- `develop branch` : 개발 브랜치, feature 브랜치가 merge됨
- `feature / chore / refactor branch` : 개발 브랜치

  <br/>

### 2️⃣ Flow

- 이슈 생성
- 이슈 번호에 맞게 `develop` 브랜치에서 새로운 브랜치 생성
- 작업 완료 후 커밋 컨벤션에 맞게 커밋
- PR 생성
- 코드 리뷰 후 `develop` 브랜치로 병합
  - 최소 1명 승인 후 `develop` 브랜치로 머지

  <br/>

### 3️⃣ Branch Naming Convention

- **구조**: `브랜치종류/#이슈번호-기능설명`
- **구분자**: 하이픈(`-`) 사용

| Prefix     | 설명               |
| ---------- | ------------------ |
| `init`     | 초기 프로젝트 세팅 |
| `feat`     | 새로운 기능 추가   |
| `fix`      | 버그 수정          |
| `docs`     | 문서 변경          |
| `style`    | UI 수정            |
| `refactor` | 코드 리팩토링      |
| `test`     | 테스트 코드 관련   |
| `chore`    | 설정 파일 변경     |
| `hotfix`   | 긴급한 버그 수정   |
| `deploy`   | 배포 관련 작업     |

  <br/>

### 4️⃣ Commit Message Convention

- **구조**: `prefix: 상세설명 (#이슈번호)`
- **예시**
  - `init: 프로젝트 초기 세팅 (#1)`
  - `feat: 근접형 적 구현 (#2)`

| Prefix     | 설명               |
| ---------- | ------------------ |
| `init`     | 초기 프로젝트 세팅 |
| `feat`     | 새로운 기능 추가   |
| `fix`      | 버그 수정          |
| `docs`     | 문서 변경          |
| `style`    | UI 수정            |
| `refactor` | 코드 리팩토링      |
| `test`     | 테스트 코드 관련   |
| `chore`    | 설정 파일 변경     |
| `hotfix`   | 긴급한 버그 수정   |
| `deploy`   | 배포 관련 작업     |

  <br/>

<div>

## 📂 프로젝트 구조

```
📦 second-terra
├─ 📁 .github
│  ├─ 📁 ISSUE_TEMPLATE
│  │  ├─ 📄 feat.md          # [FEAT] 이슈 템플릿
│  │  ├─ 📄 chore.md         # [CHORE] 이슈 템플릿
│  │  └─ 📄 bug.md           # [BUG] 이슈 템플릿
│  └─ 📄 PULL_REQUEST_TEMPLATE.md
├─ 📄 .coderabbit.yaml        # CodeRabbit 리뷰 언어 설정
├─ 📁 Assets
│  ├─ 📁 Editor
│  │  └─ 📄 EnemyTestSetup.cs # 테스트용 적 배치 에디터 툴
│  ├─ 📁 Prefabs
│  │  ├─ 📄 Projectile.prefab
│  │  ├─ 📄 Enemy_Normal.prefab
│  │  ├─ 📄 Enemy_Suicide.prefab
│  │  └─ 📄 Enemy_Dash.prefab
│  ├─ 📁 Scenes
│  │  └─ 📄 SampleScene.unity
│  └─ 📁 Scripts
│     ├─ 📁 Player
│     │  ├─ 📄 PlayerController.cs   # 이동/조작
│     │  ├─ 📄 PlayerCombat.cs       # 투사체 발사
│     │  ├─ 📄 PlayerStats.cs        # 체력
│     │  └─ 📁 Weapon                # 무기별 의체(3종) 로직 (예정)
│     ├─ 📁 Enemy
│     │  ├─ 📄 EnemyBase.cs          # 적 공통(체력, 피격 연출)
│     │  ├─ 📄 EnemyBalance.cs       # 데미지 기준값
│     │  ├─ 📄 EnemyHealthBar.cs     # 적 머리 위 체력바 UI
│     │  ├─ 📄 MeleeEnemyBase.cs     # 근접형 공통(추적/이동)
│     │  ├─ 📄 MeleeNormalEnemy.cs   # 일반형
│     │  ├─ 📄 MeleeSuicideEnemy.cs  # 자폭형
│     │  ├─ 📄 MeleeDashEnemy.cs     # 돌진형
│     │  ├─ 📁 Ranged                # 원거리형 적 (예정)
│     │  └─ 📁 Boss                  # 섹터별 보스 (예정)
│     ├─ 📁 Projectile
│     │  ├─ 📄 Projectile.cs
│     │  └─ 📄 ProjectilePool.cs     # 오브젝트 풀링
│     ├─ 📁 Interface
│     │  └─ 📄 IDamageable.cs        # 피해 처리 공통 인터페이스
│     ├─ 📁 UI                       # HUD, 스테이지 선택 등 (예정)
│     ├─ 📁 Effects                  # 피격/스킬 이펙트, VFX (예정)
│     ├─ 📁 Stage                    # 섹터/미션(섬멸·방어·호위 등) 시스템 (예정)
│     └─ 📁 Progression              # 미션 보상, 스탯 성장 (예정)
├─ 📁 Packages                       # Unity 패키지 매니페스트
└─ 📁 ProjectSettings
```

</div>
