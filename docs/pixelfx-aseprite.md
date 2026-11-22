# pixelfx 이펙트를 Aseprite에서 고치기

pixelfx 시트(PNG + JSON)를 `.aseprite` 파일로 바꿔 손으로 다듬고, Aseprite에서 내보낸 시트를 그대로 Unity로 돌려보내는 방법.

## 1. .aseprite로 변환

```bash
# 보스 스킬 이펙트 24개 전부 (팔레트 램프 포함)
python3 tools/pixelfx/to-aseprite.py Assets/PixelFx/Sheets/bosses --manifest tools/pixelfx/boss-fx.json --out records/aseprite
# 시트 하나
python3 tools/pixelfx/to-aseprite.py Assets/PixelFx/Sheets/bosses/serena-rift.json --out records/aseprite
```

파일마다 들어 있는 것:

| 항목 | 내용 |
|---|---|
| 프레임 | 시트의 모든 프레임, 각자 원래 길이(30 fps = 33 ms) |
| 레이어 | RGBA 레이어 1개(이름 = 시트 이름). 빛 번짐은 반투명 픽셀로 구워져 있음 |
| 태그 | 전체 프레임에 시트 이름으로 1개 |
| 슬라이스 `origin` | 캔버스 전체 크기, **피벗 = 이펙트 기준점**(발밑, 타격 지점). 슬라이스의 사용자 데이터(User Data)에 pixelfx 정보(id, fps, 루프 여부, 액트 타이밍)가 JSON으로 들어 있음. 지우거나 이름을 바꾸지 말 것 |
| 팔레트 | 그 이펙트의 컨셉 팔레트 램프 8색(DUSK / HALO / CRYSTAL) |

## 2. Aseprite에서 편집

- 픽셀, 프레임 길이, 프레임 추가/삭제 모두 자유. 레이어를 늘려도 됨(내보낼 때 합쳐짐).
- 기준점을 옮기려면 슬라이스 툴로 `origin` 슬라이스의 피벗을 옮긴다. Unity는 이 피벗을 그대로 따른다.
- 캔버스 크기를 바꿨다면 `origin` 슬라이스도 새 캔버스에 맞춰 둘 것.
- 루프 이펙트(후광, 영역, 광선 등)는 마지막 프레임이 첫 프레임으로 자연스럽게 이어져야 한다.

## 3. Unity로 돌려보내기

**File > Export Sprite Sheet**:

- Layout: **By Rows** (아무 배치나 읽지만 기본값이 무난)
- Output: **JSON Data**, 형식 **Array**, **Slices** 와 **Tags** 체크
- Trim은 켜도 됨(기준점이 프레임마다 보정됨)
- 파일 이름: 원래 시트 위치에 덮어쓰기. 예) `Assets/PixelFx/Sheets/bosses/serena-rift.png` + `serena-rift.json`

명령줄로는:

```bash
aseprite -b serena-rift.aseprite --sheet Assets/PixelFx/Sheets/bosses/serena-rift.png \
  --data Assets/PixelFx/Sheets/bosses/serena-rift.json --format json-array --sheet-type rows --list-tags --list-slices
```

Unity가 PNG/JSON 변경을 감지해 같은 `PixelFxSheet` 에셋을 갱신하므로(GUID 유지), 그 시트를 쓰는 스킬·프리팹이 바로
새 그림으로 바뀐다. 판정 시각은 스킬 데이터에 초 단위로 있으니, 접촉 프레임을 크게 옮겼다면
`Assets/Game/Combat/Editor/BossLibrary.cs`의 `hit` 지연값도 맞춰 줄 것.

## 주의

- Aseprite에서 내보낸 시트(JSON의 `meta.app`이 aseprite.org)는 `node tools/pixelfx/export-set.mjs`가 덮어쓰지 않고
  건너뛴다. 코드 원본으로 다시 렌더하려면 `--force`.
- 슬라이스가 없는 일반 Aseprite 시트도 가져올 수 있다. 이때 기준점은 프레임 아래 가운데, fps는 첫 프레임 길이로 정한다.
