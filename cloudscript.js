// CloudScript Initialization and AWS Setup
const AWS_REGION = "ap-northeast-2"; // AppConfig.AWSRegion
const PLAYERS_TABLE_NAME = "CardClicker_Players"; // AppConfig.PlayersTableName
const PLAYER_CARDS_TABLE_NAME = "CardClicker_PlayerCards"; // AppConfig.PlayerCardsTableName
const CARD_MASTER_TABLE_NAME = "CardClicker_CardMaster"; // AppConfig.CardMasterTableName

handlers.initializeAwsDynamoDB = () => {
    // AWS Secret Keys를 Title Internal Data에서 로드
    const titleInternalData = server.GetTitleInternalData({});
    const awsAccessKeyId = titleInternalData.Data["AwsAccessKeyId"];
    const awsSecretAccessKey = titleInternalData.Data["AwsSecretAccessKey"];

    if (!awsAccessKeyId || !awsSecretAccessKey) {
        log.error("AWS credentials not found in Title Internal Data.");
        // PlayFab에서 에러 메시지를 클라이언트로 보낼 수 있는 방법을 찾아야 함
        return { error: "AWS credentials not configured." };
    }

    // AWS SDK 초기화 (PlayFab은 기본적으로 AWS SDK v2를 지원합니다.)
    AWS.config.update({
        accessKeyId: awsAccessKeyId,
        secretAccessKey: awsSecretAccessKey,
        region: AWS_REGION
    });

    return new AWS.DynamoDB.DocumentClient();
};

// PlayFab 클라이언트가 호출할 수 있는 함수
// ==================== Player CRUD ====================

handlers.createPlayer = (args, context) => {
    const player = args.player;
    if (!player || !player.UserId) {
        return { error: "Player object with UserId is required." };
    }

    const docClient = handlers.initializeAwsDynamoDB();
    if (docClient.error) {
        return docClient;
    }

    const item = {
        userId: player.UserId,
        clickCount: player.ClickCount || 0,
        dust: player.Dust || 0,
        totalClicks: player.TotalClicks || 0,
        deckPower: player.DeckPower || 0,
        lastSaveTime: player.LastSaveTime || new Date().toISOString(),
        deckCardIds: player.DeckCardIds || []
    };

    const params = {
        TableName: PLAYERS_TABLE_NAME,
        Item: item
    };

    return new Promise((resolve, reject) => {
        docClient.put(params, (err, data) => {
            if (err) {
                log.error(`플레이어 생성 실패: ${err.message}`);
                reject({ error: `플레이어 생성 실패: ${err.message}` });
            } else {
                resolve({ success: true });
            }
        });
    });
};

handlers.getPlayer = (args, context) => {
    const userId = args.userId;
    if (!userId) {
        return { error: "userId is required." };
    }

    const docClient = handlers.initializeAwsDynamoDB();
    if (docClient.error) {
        return docClient;
    }

    const params = {
        TableName: PLAYERS_TABLE_NAME,
        Key: {
            "userId": userId
        }
    };

    return new Promise((resolve, reject) => {
        docClient.get(params, (err, data) => {
            if (err) {
                log.error(`플레이어 조회 실패: ${err.message}`);
                reject({ error: `플레이어 조회 실패: ${err.message}` });
            } else {
                if (!data.Item) {
                    resolve({ player: null });
                } else {
                    const playerItem = data.Item;
                    resolve({
                        player: {
                            UserId: playerItem.userId,
                            ClickCount: playerItem.clickCount,
                            Dust: playerItem.dust,
                            TotalClicks: playerItem.totalClicks,
                            DeckPower: playerItem.deckPower,
                            LastSaveTime: playerItem.lastSaveTime,
                            DeckCardIds: playerItem.deckCardIds || []
                        }
                    });
                }
            }
        });
    });
};

handlers.updatePlayer = (args, context) => {
    const player = args.player;
    if (!player || !player.UserId) {
        return { error: "Player object with UserId is required." };
    }

    const docClient = handlers.initializeAwsDynamoDB();
    if (docClient.error) {
        return docClient;
    }
    
    player.LastSaveTime = new Date().toISOString();

    const params = {
        TableName: PLAYERS_TABLE_NAME,
        Key: {
            "userId": player.UserId
        },
        UpdateExpression: "SET clickCount = :cc, dust = :d, totalClicks = :tc, deckPower = :dp, lastSaveTime = :lst, deckCardIds = :deck",
        ExpressionAttributeValues: {
            ":cc": player.ClickCount,
            ":d": player.Dust,
            ":tc": player.TotalClicks,
            ":dp": player.DeckPower,
            ":lst": player.LastSaveTime,
            ":deck": player.DeckCardIds || []
        }
    };

    return new Promise((resolve, reject) => {
        docClient.update(params, (err, data) => {
            if (err) {
                log.error(`플레이어 업데이트 실패: ${err.message}`);
                reject({ error: `플레이어 업데이트 실패: ${err.message}` });
            } else {
                resolve({ success: true });
            }
        });
    });
};

// ==================== PlayerCard CRUD ====================

handlers.addPlayerCard = (args, context) => {
    const card = args.card;
    if (!card || !card.UserId || !card.InstanceId || !card.CardId) {
        return { error: "PlayerCard object with UserId, InstanceId, CardId is required." };
    }

    const docClient = handlers.initializeAwsDynamoDB();
    if (docClient.error) {
        return docClient;
    }

    const item = {
        userId: card.UserId,
        instanceId: card.InstanceId,
        cardId: card.CardId,
        level: card.Level || 1,
        acquiredAt: card.AcquiredAt || new Date().toISOString(),
        isNew: card.IsNew || true
    };

    const params = {
        TableName: PLAYER_CARDS_TABLE_NAME,
        Item: item
    };

    return new Promise((resolve, reject) => {
        docClient.put(params, (err, data) => {
            if (err) {
                log.error(`카드 추가 실패: ${err.message}`);
                reject({ error: `카드 추가 실패: ${err.message}` });
            } else {
                resolve({ success: true });
            }
        });
    });
};

handlers.getPlayerCards = (args, context) => {
    const userId = args.userId;
    if (!userId) {
        return { error: "userId is required." };
    }

    const docClient = handlers.initializeAwsDynamoDB();
    if (docClient.error) {
        return docClient;
    }

    const params = {
        TableName: PLAYER_CARDS_TABLE_NAME,
        KeyConditionExpression: "userId = :uid",
        ExpressionAttributeValues: {
            ":uid": userId
        }
    };

    return new Promise((resolve, reject) => {
        docClient.query(params, (err, data) => {
            if (err) {
                log.error(`카드 조회 실패: ${err.message}`);
                reject({ error: `카드 조회 실패: ${err.message}` });
            } else {
                const cards = (data.Items || []).map(item => ({
                    UserId: item.userId,
                    InstanceId: item.instanceId,
                    CardId: item.cardId,
                    Level: item.level,
                    AcquiredAt: item.acquiredAt,
                    IsNew: item.isNew || false
                }));
                resolve({ cards: cards });
            }
        });
    });
};

handlers.upgradeCard = (args, context) => {
    const { userId, instanceId, newLevel } = args;
    if (!userId || !instanceId || newLevel === undefined) {
        return { error: "userId, instanceId, and newLevel are required." };
    }

    const docClient = handlers.initializeAwsDynamoDB();
    if (docClient.error) {
        return docClient;
    }

    const params = {
        TableName: PLAYER_CARDS_TABLE_NAME,
        Key: {
            "userId": userId,
            "instanceId": instanceId
        },
        UpdateExpression: "SET #lvl = :newLevel",
        ExpressionAttributeNames: {
            "#lvl": "level"
        },
        ExpressionAttributeValues: {
            ":newLevel": newLevel
        }
    };

    return new Promise((resolve, reject) => {
        docClient.update(params, (err, data) => {
            if (err) {
                log.error(`카드 강화 실패: ${err.message}`);
                reject({ error: `카드 강화 실패: ${err.message}` });
            } else {
                resolve({ success: true });
            }
        });
    });
};

handlers.updateCardIsNew = (args, context) => {
    const { userId, instanceId, isNew } = args;
    if (!userId || !instanceId || isNew === undefined) {
        return { error: "userId, instanceId, and isNew are required." };
    }

    const docClient = handlers.initializeAwsDynamoDB();
    if (docClient.error) {
        return docClient;
    }

    const params = {
        TableName: PLAYER_CARDS_TABLE_NAME,
        Key: {
            "userId": userId,
            "instanceId": instanceId
        },
        UpdateExpression: "SET isNew = :isNew",
        ExpressionAttributeValues: {
            ":isNew": isNew
        }
    };

    return new Promise((resolve, reject) => {
        docClient.update(params, (err, data) => {
            if (err) {
                log.error(`카드 isNew 업데이트 실패: ${err.message}`);
                reject({ error: `카드 isNew 업데이트 실패: ${err.message}` });
            } else {
                resolve({ success: true });
            }
        });
    });
};

handlers.deleteCard = (args, context) => {
    const { userId, instanceId } = args;
    if (!userId || !instanceId) {
        return { error: "userId and instanceId are required." };
    }

    const docClient = handlers.initializeAwsDynamoDB();
    if (docClient.error) {
        return docClient;
    }

    const params = {
        TableName: PLAYER_CARDS_TABLE_NAME,
        Key: {
            "userId": userId,
            "instanceId": instanceId
        }
    };

    return new Promise((resolve, reject) => {
        docClient.delete(params, (err, data) => {
            if (err) {
                log.error(`카드 삭제 실패: ${err.message}`);
                reject({ error: `카드 삭제 실패: ${err.message}` });
            } else {
                resolve({ success: true });
            }
        });
    });
};

// ==================== CardMaster ====================

handlers.getCardMaster = (args, context) => {
    const cardId = args.cardId;
    if (!cardId) {
        return { error: "cardId is required." };
    }

    const docClient = handlers.initializeAwsDynamoDB();
    if (docClient.error) {
        return docClient;
    }

    const params = {
        TableName: CARD_MASTER_TABLE_NAME,
        Key: {
            "cardId": cardId
        }
    };

    return new Promise((resolve, reject) => {
        docClient.get(params, (err, data) => {
            if (err) {
                log.error(`카드 마스터 조회 실패: ${err.message}`);
                reject({ error: `카드 마스터 조회 실패: ${err.message}` });
            } else {
                if (!data.Item) {
                    resolve({ cardMaster: null });
                } else {
                    const item = data.Item;
                    resolve({
                        cardMaster: {
                            CardId: item.cardId,
                            Name: item.name,
                            Rarity: item.rarity,
                            HP: item.hp,
                            ATK: item.atk,
                            DEF: item.def,
                            Ability: item.ability || "NONE"
                        }
                    });
                }
            }
        });
    });
};

handlers.getRandomCardId = (args, context) => {
    // AppConfig.TotalCardCount 값을 사용해야 합니다.
    // CloudScript는 직접 AppConfig.cs를 읽을 수 없으므로,
    // 이 값은 PlayFab Title Data에 저장되어 있어야 합니다.
    // 일단은 하드코딩된 값으로 대체합니다.
    const TOTAL_CARD_COUNT = 1000; // 사용자님의 AppConfig.TotalCardCount 값

    const random = Math.floor(Math.random() * TOTAL_CARD_COUNT) + 1;
    const cardId = `card_${String(random).padStart(4, '0')}`;
    return { cardId: cardId };
};

// ==================== 헬퍼 함수 ====================
// PlayFab CloudScript는 C#의 DateTime.UtcNow.ToString("o")와 같은
// ISO 8601 형식의 날짜 문자열을 직접 생성하는 내장 함수가 없으므로,
// 필요한 경우 JavaScript의 Date 객체를 활용해야 합니다.
// 예를 들어, new Date().toISOString()을 사용할 수 있습니다.