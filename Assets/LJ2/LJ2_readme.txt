레이싱 미니게임

모바일빌드
맵에 start, goal포인트를 묶어 track구성(하나의 맵에 여러 track 구성가능)
맵보다 작게 cinemachine track 설정 하여 따라오도록

정해진 색 차량 생성

input Vector2로 (조이스틱) 조작
	현재 방향과 조작 방향이 많이(조정 가능) 다르면 드리프트(QuaternionLerp)

순위에 따라 점수++

Track
	기능 
	시작, 도착 위치 선정 및 판정


RacingManager
	기능
	맵, 트랙 랜덤 설정
	UI애니매이션으로 카운트다운
	최초 도착 후 retireTime 설정
	순위에 따른 점수 부여 (UI로 보여주기, GameManager(?)에 넘겨주기)
	씬 전환 요청
	
RacingController
	기능
	inputVector2를 받아서 움직임
	현재의 Forward와 inputVector2가 많이(조절가능) 다를 경우 QuaternionLerp 동안 드리프트
		바퀴자국(얘도 파티클 인가)......연기(파티클)......

