# 카드 클리커 RPG

클릭만으로 카드를 모아 성장하고, 최고의 자리에 도전하는 간단하면서도 중독성 있는 RPG 게임입니다.

## 🎮 게임 소개

당신은 계속해서 클릭하여 새로운 카드를 획득하고, 덱을 강화하여 더 높은 전투력을 달성해야 합니다. 다양한 능력을 가진 카드들을 전략적으로 조합하여 효율을 극대화하고, 다른 플레이어들과 전투력 랭킹으로 경쟁하세요!

## ✨ 주요 기능

### 🖱️ **클릭 & 카드 획득**
*   화면을 클릭하여 재화를 모으고, 일정 수치를 달성할 때마다 새로운 카드를 랜덤하게 획득합니다.

### 🃏 **다양한 카드와 능력**
*   **등급**: `common`, `rare`, `epic`, `legendary` 등 다양한 등급의 카드가 존재합니다.
*   **능력**: 카드들은 저마다 고유한 능력을 가지고 있습니다.
    *   `AUTO_CLICK`: 5초마다 자동으로 클릭해줍니다.
    *   `CLICK_MULTIPLY`: 클릭 당 배율을 증가시킵니다.
    *   `DUST_BONUS`: 카드 분해 시 더 많은 가루를 획득합니다.
    *   `UPGRADE_DISCOUNT`: 카드 강화 비용을 할인해줍니다.
    *   `LUCKY`: 카드 획득 시 더 높은 등급의 카드가 나올 확률을 높여줍니다.

### 덱 편성 & 전투력
*   보유한 카드 중 가장 강력한 5장으로 자동으로 덱이 편성됩니다.
*   덱의 총 전투력은 PlayFab 리더보드에 기록되어 다른 플레이어들과 순위를 경쟁하게 됩니다.

### ✨ **카드 강화 & 분해**
*   **분해**: 필요 없는 카드를 분해하여 '가루'를 획득할 수 있습니다.
*   **강화**: '가루'를 사용하여 카드의 레벨을 올려 더욱 강력하게 만들 수 있습니다.

### 🏆 **랭킹 시스템**
*   PlayFab 리더보드를 통해 다른 플레이어들과 덱 전투력 순위를 실시간으로 비교하고 경쟁할 수 있습니다.

## ⚙️ 시스템 아키텍처

*   **인증 및 랭킹**: [PlayFab](https://playfab.com/) 서비스를 사용하여 플레이어 계정 관리와 리더보드 기능을 구현합니다.
*   **데이터베이스**: 플레이어 정보, 카드 데이터 등 핵심 데이터는 **AWS DynamoDB**에 저장됩니다.
*   **서버 로직**: 클라이언트가 데이터베이스에 직접 접근하는 위험을 막기 위해, 모든 서버 로직은 **PlayFab CloudScript**를 통해 안전하게 실행됩니다. 게임 클라이언트는 CloudScript에 요청을 보내고, CloudScript가 AWS DynamoDB와 통신하여 결과를 반환하는 안전한 구조로 되어있습니다.

## 🚀 설치 및 실행 방법

1.  **선수 조건**:
    *   [.NET SDK](https://dotnet.microsoft.com/download)가 설치되어 있어야 합니다.
    *   [PlayFab 계정](https://developer.playfab.com/) 및 게임 타이틀이 생성되어 있어야 합니다.
    *   AWS DynamoDB 테이블(`CardClicker_Players`, `CardClicker_PlayerCards`, `CardClicker_CardMaster`)이 설정되어 있어야 합니다.
    *   PlayFab 타이틀의 **[Content] -> [Title Internal Data]**에 `AwsAccessKeyId`와 `AwsSecretAccessKey`가 저장되어 있어야 합니다.
    *   `cloudscript.js` 파일이 PlayFab CloudScript에 업로드되고 배포되어 있어야 합니다.

2.  **프로젝트 클론**:
    ```bash
    git clone [프로젝트_레포지토리_URL]
    cd CardClickerRPG
    ```

3.  **의존성 설치 및 빌드**:
    ```bash
    dotnet restore
    dotnet build
    ```

4.  **게임 실행**:
    ```bash
    dotnet run
    ```

## 🧑‍💻 개발 환경

*   **언어**: C#
*   **프레임워크**: .NET (Console Application)
*   **백엔드 서비스**: PlayFab, AWS DynamoDB
*   **IDE**: Visual Studio 또는 VS Code (권장)

## 🤝 기여 방법

이 프로젝트는 개인 학습 및 개발 목적으로 시작되었습니다. 현재는 외부 기여를 받지 않습니다.
