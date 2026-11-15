# Firework 씬 사용법

대상 프로젝트: `C:\Users\cheon\SourceTreeRepository\CapstoneEngine01_Firework`

현재 Unity 버전: **6000.3.23f1**, URP **17.3.0**. 2026-11-15에 6000.6.1f1에서 호환 설정으로 이식하고 불꽃 10종과 발사 패턴 3종을 재검증했습니다.

`Assets/Scenes/Firework.unity`를 열고 Play를 누르면 쇼가 반복됩니다. Hierarchy의 **Firework Show Manager**를 선택해 설정합니다.

## 모양·색상·발사 패턴 조합

| 항목 | 사용법 |
|---|---|
| Shapes | 원본에서 가져온 모양 프리팹 10개. 프리팹을 복제하고 Particle System 모듈을 수정해 새 모양을 추가할 수 있습니다. |
| Palettes | 0: Gold / Amber, 1: Ice / Cyan, 2: Rose / Violet. Gradient의 색상 키로 수정·추가합니다. 투명도는 원본 효과의 곡선을 보존하므로 팔레트의 Alpha 키는 사용하지 않습니다. |
| Launch Points | Left / Center / Right 빈 오브젝트의 위치·회전을 바꾸거나 새 Transform을 배열에 연결합니다. 배경 메시가 없는 발사 기준점입니다. |
| Cues → Shape | 사용할 Particle System 프리팹을 직접 끌어 넣습니다. Shapes 배열과 별도로 지정하므로 목록을 바꿀 때 Cue의 Shape도 확인하세요. |
| Cues → Palette Index | -1: 원본 색상. 0 이상: Palettes 배열 인덱스. 범위를 벗어나면 원본 색상을 사용합니다. |
| Cues → Pattern | Sequential: 왼쪽부터 순차 발사. Simultaneous: 같은 프레임에 동시 발사. Alternating: 양끝부터 교대로 발사. |
| Cues → Count / Interval | 프리팹 생성 횟수(1~12) / 순차·교차 발사 사이의 초 단위 간격. 프리팹 내부의 방출 설정에 따라 실제 불꽃 수는 더 많을 수 있습니다. |
| Cues → Delay | 직전 Cue의 마지막 프리팹 발사 이후 대기 시간. 첫 Cue에서는 쇼 시작 이후 대기 시간입니다. |
| Cues → Emission Duration | 발사 이미터 방출을 멈출 때까지의 시간. 이미 발사된 불꽃은 남은 수명 동안 표시됩니다. 원본의 비반복 프리팹은 이 시간보다 먼저 방출을 마칠 수 있습니다. |
| Loop / Loop Pause | 시간표 반복 여부 / 한 회차 후 추가 대기 시간. 잔여 불꽃과 다음 회차는 겹칠 수 있습니다. |
| Max Active Effects | 동시에 유지할 프리팹 인스턴스 상한. 상한을 넘는 발사는 건너뜁니다. |

기본 시간표는 원본 도입부 → 금색 동시 발사 → 청록색 교차 발사 → 분홍/보라색 피날레입니다. 음악에 맞춰 수 분간 구성된 원본 시간표는 무음 미리보기용으로 짧게 재구성했습니다.

## 재사용과 스크립트

- `Assets/Fireworks/Scripts/FireworkShowManager.cs`: 시간표, 패턴, 색상 변경, 발사 위치, 효과 정리를 담당합니다.
- `Assets/Fireworks/Reference/Prefabs`: 원본 파티클 프리팹 10개. 서브 이미터를 유지하고 사운드·조명·보조 메시 컴포넌트를 제거했습니다.
- `Assets/Fireworks/Reference/Materials`, `Textures`: 재사용한 머티리얼과 텍스처. 머티리얼은 URP Particles/Unlit로 변환했습니다.
- 매니저 컴포넌트 메뉴의 **Play Show (Play Mode)** / **Stop And Clear (Play Mode)**로 재시작하거나 모두 정리합니다.
- 코드에서 `Launch(shapeIndex, paletteIndex, LaunchPattern, count)`로 개별 패턴을 실행할 수 있습니다. Play 모드에서 활성화된 매니저에 호출하세요.
- 자동 정리는 방출 종료 후 잔여 파티클이 모두 사라지면 실행됩니다. 비정상적으로 계속 살아 있는 하위 이미터는 최대 30초 뒤 정리됩니다.

## 수변 도시 환경과 카메라

추가 요청에 따라 `Assets/Environment/Miami 2525.obj`를 중앙 수로의 맞은편과 양쪽에 배치했습니다. 제공된 건물·도로 텍스처를 URP 머티리얼로 연결했으며 `1ab2.jpg`를 움직이는 수면 재질에 사용합니다. 하늘 사진은 첨부되어 있지 않아 별이 드문 야간 하늘 셰이더를 구성했습니다. 환경 오브젝트는 **Harbor Environment** 아래에 모여 있습니다.

- `Firework Show Manager → Effect Scale`: 불꽃의 높이와 폭발 크기 배율. 현재 1.5입니다.
- 발사 기준점은 기존 중앙 부근의 수면 위에 있으며, X 위치는 -32 / 0 / 32, Y는 0.5입니다.
- Game 창을 클릭한 뒤 **마우스 우클릭을 누른 상태에서** 마우스로 시점을 회전하고 **WASD**로 이동합니다.
- 우클릭 중 **Q/E**: 하강/상승, **Shift**: 빠른 이동, **휠**: 이동 속도 조절.
- **Home**: 초기 관람 위치 복귀, 우클릭 해제: 마우스 커서 반환.
- 플레이어 캐릭터나 충돌 이동은 없습니다. 카메라의 높이는 수면 아래로 내려가지 않도록 제한했습니다.

## 배경음악

**City BGM - Loop** 오브젝트의 Audio Source에 `Assets/Bgm/city_bgm.mp3`를 연결했습니다. **Play On Awake**와 **Loop**가 켜져 있어 게임 실행 즉시 반복 재생됩니다. 2D 오디오이므로 카메라 이동에 따라 음량이 달라지지 않습니다. 기본 Volume은 0.35이며 Inspector에서 바꿀 수 있습니다. Audio Listener는 Main Camera 하나에만 있습니다.

첨부 OBJ에는 일부 self-intersecting polygon을 제외했다는 Unity 가져오기 경고가 있습니다. 원본 파일은 수정하지 않았으며, 가져온 모델과 새 환경 셰이더의 재생 화면을 확인했습니다.

원본 프로젝트는 수정하지 않았습니다. 대상 씬의 작업 전 디스크 사본은 `Assets/Fireworks/Backup/Firework.before-setup.txt`에 보관했습니다. 에디터 제어를 위해 대상 프로젝트에 `com.unity.pipeline` 1.0.0을 추가했으며, Unity CLI는 요청대로 기존 버전을 유지했습니다.
