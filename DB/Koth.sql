-- --------------------------------------------------------
-- Хост:                         127.0.0.1
-- Версия сервера:               8.0.36 - MySQL Community Server - GPL
-- Операционная система:         Win64
-- HeidiSQL Версия:              12.6.0.6765
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

-- Дамп структуры для таблица koth.bans
CREATE TABLE IF NOT EXISTS `bans` (
  `id` int NOT NULL AUTO_INCREMENT,
  `player_uid` varchar(36) DEFAULT NULL,
  `active` tinyint(1) DEFAULT '1',
  `ban_reason` varchar(200) DEFAULT NULL,
  `ban_date` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `player_uid` (`player_uid`),
  CONSTRAINT `bans_ibfk_1` FOREIGN KEY (`player_uid`) REFERENCES `players` (`player_uid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Дамп данных таблицы koth.bans: ~0 rows (приблизительно)

-- Дамп структуры для таблица koth.bonus_codes
CREATE TABLE IF NOT EXISTS `bonus_codes` (
  `code` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `multiplier` decimal(5,2) NOT NULL,
  `date_end` datetime NOT NULL,
  `max_uses` int NOT NULL DEFAULT '1',
  `uses` int NOT NULL DEFAULT '0',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Дамп данных таблицы koth.bonus_codes: ~1 rows (приблизительно)
INSERT IGNORE INTO `bonus_codes` (`code`, `name`, `multiplier`, `date_end`, `max_uses`, `uses`, `created_at`) VALUES
	('Testing', 'test', 5.00, '2025-09-19 21:02:56', 10000, 5, '2025-09-05 16:03:27');

-- Дамп структуры для таблица koth.players
CREATE TABLE IF NOT EXISTS `players` (
  `player_uid` varchar(36) NOT NULL,
  `player_name` varchar(100) DEFAULT NULL,
  `profile_name` varchar(100) DEFAULT NULL,
  `platform_name` varchar(50) DEFAULT NULL,
  `machine_name` varchar(100) DEFAULT NULL,
  `adapter_name` varchar(100) DEFAULT NULL,
  `money` int DEFAULT '1000',
  `level` int DEFAULT '1',
  `xp` int DEFAULT '0',
  `kills` int DEFAULT '0',
  `deaths` int DEFAULT '0',
  `friendly_kills` int DEFAULT '0',
  PRIMARY KEY (`player_uid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Дамп данных таблицы koth.players: ~0 rows (приблизительно)

-- Дамп структуры для таблица koth.player_bonuses
CREATE TABLE IF NOT EXISTS `player_bonuses` (
  `player_uid` varchar(36) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `code` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `activated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_uid`,`code`),
  KEY `code` (`code`),
  CONSTRAINT `player_bonuses_ibfk_1` FOREIGN KEY (`player_uid`) REFERENCES `players` (`player_uid`) ON DELETE CASCADE,
  CONSTRAINT `player_bonuses_ibfk_2` FOREIGN KEY (`code`) REFERENCES `bonus_codes` (`code`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Дамп данных таблицы koth.player_bonuses: ~0 rows (приблизительно)

-- Дамп структуры для таблица koth.player_items
CREATE TABLE IF NOT EXISTS `player_items` (
  `id` int NOT NULL AUTO_INCREMENT,
  `player_uid` varchar(36) DEFAULT NULL,
  `item_name` varchar(200) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `player_uid` (`player_uid`),
  CONSTRAINT `player_items_ibfk_1` FOREIGN KEY (`player_uid`) REFERENCES `players` (`player_uid`)
) ENGINE=InnoDB AUTO_INCREMENT=11152 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Дамп данных таблицы koth.player_items: ~0 rows (приблизительно)

-- Дамп структуры для таблица koth.player_presets
CREATE TABLE IF NOT EXISTS `player_presets` (
  `id` int NOT NULL AUTO_INCREMENT,
  `player_uid` varchar(36) DEFAULT NULL,
  `primary_weapon` varchar(200) DEFAULT NULL,
  `secondary_weapon` varchar(200) DEFAULT NULL,
  `optic_weapon` varchar(200) DEFAULT NULL,
  `muzzle` varchar(200) DEFAULT NULL,
  `launcher` varchar(200) DEFAULT NULL,
  `throwable` text,
  `range_finder` varchar(200) DEFAULT NULL,
  `viperhood` varchar(200) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `player_uid` (`player_uid`),
  CONSTRAINT `player_presets_ibfk_1` FOREIGN KEY (`player_uid`) REFERENCES `players` (`player_uid`)
) ENGINE=InnoDB AUTO_INCREMENT=32 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Дамп данных таблицы koth.player_presets: ~0 rows (приблизительно)

-- Дамп структуры для таблица koth.player_stats
CREATE TABLE IF NOT EXISTS `player_stats` (
  `player_uid` varchar(36) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `bullets_shot` int NOT NULL DEFAULT '0',
  `grenades_thrown` int NOT NULL DEFAULT '0',
  `max_kill_streak` int NOT NULL DEFAULT '0',
  `max_kill_distance` int NOT NULL DEFAULT '0',
  `insertion_bonus` int NOT NULL DEFAULT '0',
  `kill_streak_x3` int NOT NULL DEFAULT '0',
  `kill_streak_x5` int NOT NULL DEFAULT '0',
  `kill_streak_x10` int NOT NULL DEFAULT '0',
  `kill_streak_x20` int NOT NULL DEFAULT '0',
  `kill_streak_x30` int NOT NULL DEFAULT '0',
  `updated_at` timestamp NULL DEFAULT (now()) ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_uid`),
  CONSTRAINT `player_stats_ibfk_1` FOREIGN KEY (`player_uid`) REFERENCES `players` (`player_uid`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Дамп данных таблицы koth.player_stats: ~0 rows (приблизительно)

/*!40103 SET TIME_ZONE=IFNULL(@OLD_TIME_ZONE, 'system') */;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;
