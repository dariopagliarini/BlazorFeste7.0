-- --------------------------------------------------------
-- Host:                         127.0.0.1
-- Versione server:              10.4.22-MariaDB - mariadb.org binary distribution
-- S.O. server:                  Win64
-- HeidiSQL Versione:            12.7.0.6850
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;


-- Dump della struttura del database BlazorFeste
CREATE DATABASE IF NOT EXISTS `blazorfeste` /*!40100 DEFAULT CHARACTER SET utf8mb4 */;
USE `BlazorFeste`;

-- Dump della struttura di tabella BlazorFeste.anagr_casse
CREATE TABLE IF NOT EXISTS `anagr_casse` (
  `IdPrimaryKey` int(11) NOT NULL AUTO_INCREMENT,
  `IdListino` int(11) NOT NULL,
  `IdCassa` int(11) NOT NULL,
  `Cassa` varchar(50) DEFAULT '0',
  `Abilitata` bit(1) DEFAULT b'0',
  `Visibile` bit(1) DEFAULT b'0',
  `POS` bit(1) DEFAULT b'0',
  `PortName` varchar(1024) DEFAULT NULL,
  `IsRemote` bit(1) DEFAULT NULL,
  `RemoteAddress` varchar(50) DEFAULT NULL,
  `Prodotti` varchar(1024) DEFAULT '0',
  `BackColor` varchar(50) DEFAULT NULL,
  `ForeColor` varchar(50) DEFAULT NULL,
  `SoloBanco` bit(1) DEFAULT NULL,
  `ScontrinoAbilitato` bit(1) DEFAULT b'1',
  `ScontrinoMuto` bit(1) DEFAULT b'0',
  PRIMARY KEY (`IdPrimaryKey`) USING BTREE,
  UNIQUE KEY `IdListino_IdCassa` (`IdListino`,`IdCassa`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=latin1 COMMENT='Locale o Remota deve essere interpretato dal punto di vista della Cassa (cioè del Client)\r\n  - Se la stampante è Remota recupero l''indirizzo nel campo RemoteAddress\r\n  - Se la stampante è Locale significa che è attaccata al PC Client da cui stò compilando l''ordine\r\n\r\nLa stampa è gestita dal programma "PrinterServerAPI" (in ascolto sulla porta 5001) che deve essere in esecuzione su ogni PC a cui è attaccata almeno una delle stampanti termiche che stampano gli scontrini.';

-- L’esportazione dei dati non era selezionata.

-- Dump della struttura di tabella BlazorFeste.anagr_clients
CREATE TABLE IF NOT EXISTS `anagr_clients` (
  `IndirizzoIP` varchar(24) NOT NULL,
  `Livello` int(10) unsigned DEFAULT NULL,
  PRIMARY KEY (`IndirizzoIP`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- L’esportazione dei dati non era selezionata.

-- Dump della struttura di tabella BlazorFeste.anagr_liste
CREATE TABLE IF NOT EXISTS `anagr_liste` (
  `IdPrimaryKey` int(11) NOT NULL AUTO_INCREMENT,
  `IdListino` int(11) NOT NULL DEFAULT 1,
  `IdLista` int(11) NOT NULL,
  `Abilitata` bit(1) DEFAULT b'0',
  `Visibile` bit(1) DEFAULT NULL,
  `Lista` varchar(50) DEFAULT '0',
  `IdListaPadre` int(11) NOT NULL DEFAULT 0,
  `Priorità` int(11) DEFAULT 0,
  `IoSonoListaPadre` bit(1) DEFAULT b'0',
  `BackColor` varchar(50) DEFAULT NULL,
  `ForeColor` varchar(50) DEFAULT NULL,
  `Tavolo_StampaScontrino` bit(1) DEFAULT b'0',
  `Banco_StampaScontrino` bit(1) DEFAULT b'0',
  `Cucina_StampaScontrino` bit(1) DEFAULT b'0',
  `Cucina_NumeroScontrini` int(11) DEFAULT NULL,
  `IdStampante` int(11) unsigned DEFAULT NULL,
  `StampaNoteOrdine` bit(1) DEFAULT b'0',
  PRIMARY KEY (`IdPrimaryKey`) USING BTREE,
  UNIQUE KEY `IdListino_IdLista` (`IdListino`,`IdLista`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=latin1 COMMENT='Liste di distribuzione dei prodotti';

-- L’esportazione dei dati non era selezionata.

-- Dump della struttura di tabella BlazorFeste.anagr_listini
CREATE TABLE IF NOT EXISTS `anagr_listini` (
  `IdListino` int(11) NOT NULL,
  `Listino` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`IdListino`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- L’esportazione dei dati non era selezionata.

-- Dump della struttura di tabella BlazorFeste.anagr_menu
CREATE TABLE IF NOT EXISTS `anagr_menu` (
  `IdMenu` int(11) NOT NULL,
  `Menu` varchar(50) DEFAULT NULL,
  `Icona` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`IdMenu`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- L’esportazione dei dati non era selezionata.

-- Dump della struttura di tabella BlazorFeste.anagr_prodotti
CREATE TABLE IF NOT EXISTS `anagr_prodotti` (
  `IdListino` int(11) NOT NULL DEFAULT 1,
  `IdProdotto` int(11) NOT NULL,
  `NomeProdotto` varchar(255) DEFAULT '',
  `PrezzoUnitario` double DEFAULT 5,
  `Stato` bit(1) NOT NULL,
  `IdLista` int(11) NOT NULL DEFAULT 1,
  `Magazzino` int(10) unsigned NOT NULL DEFAULT 0,
  `Consumo` int(10) unsigned NOT NULL COMMENT 'Progressivo Prodotti Ordinati',
  `Evaso` int(10) unsigned NOT NULL COMMENT 'Progressivo Prodotti Evasi',
  `ConsumoCumulativo` int(10) unsigned NOT NULL,
  `EvasoCumulativo` int(11) unsigned NOT NULL DEFAULT 0,
  `EvadiSuIdProdotto` int(11) NOT NULL DEFAULT 0,
  `BackColor` varchar(50) DEFAULT NULL,
  `ForeColor` varchar(50) DEFAULT NULL,
  `PrintQueueTicket` bit(1) NOT NULL,
  `ViewLableDaEvadere` bit(1) NOT NULL,
  `IdMenu` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`IdListino`,`IdProdotto`),
  KEY `IdLista` (`IdLista`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- L’esportazione dei dati non era selezionata.

-- Dump della struttura di tabella BlazorFeste.anagr_stampanti
CREATE TABLE IF NOT EXISTS `anagr_stampanti` (
  `IdStampante` int(10) unsigned NOT NULL,
  `SerialPort` varchar(50) CHARACTER SET latin1 DEFAULT '0',
  `IsRemote` bit(1) DEFAULT NULL,
  `RemoteAddress` varchar(50) CHARACTER SET latin1 DEFAULT NULL,
  `Note` varchar(1024) DEFAULT NULL,
  PRIMARY KEY (`IdStampante`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Locale o Remota deve essere interpretato dal punto di vista della Cassa (cioè del Client)\r\n  - Se la stampante è Remota recupero l''indirizzo nel campo RemoteAddress\r\n  - Se la stampante è Locale significa che è attaccata al PC Client da cui stò compilando l''ordine\r\n\r\nLa stampa è gestita dal programma "PrinterServerAPI" (in ascolto sulla porta 5001) che deve essere in esecuzione su ogni PC a cui è attaccata almeno una delle stampanti termiche che stampano gli scontrini.';

-- L’esportazione dei dati non era selezionata.

-- Dump della struttura di tabella BlazorFeste.arch_feste
CREATE TABLE IF NOT EXISTS `arch_feste` (
  `IdFesta` int(11) NOT NULL AUTO_INCREMENT,
  `Festa` varchar(50) NOT NULL,
  `Associazione` varchar(50) NOT NULL,
  `IdListino` int(11) NOT NULL DEFAULT 1,
  `DataInizio` date DEFAULT NULL,
  `DataFine` date DEFAULT NULL,
  `Visibile` bit(1) NOT NULL,
  `FestaAttiva` bit(1) NOT NULL,
  `Ricevuta_Riga0` varchar(40) NOT NULL DEFAULT '',
  `Ricevuta_Riga1` varchar(40) NOT NULL DEFAULT '',
  `Ricevuta_Riga2` varchar(40) NOT NULL DEFAULT '',
  `Ricevuta_Riga3` varchar(40) NOT NULL DEFAULT '',
  `Ricevuta_Riga4` varchar(40) NOT NULL DEFAULT '',
  `WebAppAttiva` bit(1) NOT NULL,
  PRIMARY KEY (`IdFesta`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=latin1;

-- L’esportazione dei dati non era selezionata.

-- Dump della struttura di tabella BlazorFeste.arch_ordini
CREATE TABLE IF NOT EXISTS `arch_ordini` (
  `IdOrdine` bigint(20) NOT NULL AUTO_INCREMENT,
  `Cassa` char(1) NOT NULL,
  `DataOra` datetime NOT NULL,
  `TipoOrdine` varchar(255) NOT NULL,
  `Tavolo` varchar(255) NOT NULL,
  `NumeroCoperti` varchar(255) NOT NULL,
  `IdStatoOrdine` int(11) NOT NULL DEFAULT 0,
  `Timestamp` timestamp NOT NULL DEFAULT current_timestamp(),
  `DataAssegnazione` datetime NOT NULL DEFAULT current_timestamp(),
  `Referente` varchar(128) NOT NULL,
  `NoteOrdine` varchar(512) NOT NULL,
  `ProgressivoSerata` int(11) NOT NULL DEFAULT 0,
  `IdFesta` int(11) NOT NULL,
  `PagamentoConPOS` bit(1) NOT NULL DEFAULT b'0',
  `AppIdOrdine` bigint(20) NOT NULL DEFAULT 0,
  PRIMARY KEY (`IdOrdine`),
  KEY `DataAssegnazione` (`DataAssegnazione`),
  KEY `FK_arch_ordini_arch_feste` (`IdFesta`),
  CONSTRAINT `FK_arch_ordini_arch_feste` FOREIGN KEY (`IdFesta`) REFERENCES `arch_feste` (`IdFesta`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=latin1;

-- L’esportazione dei dati non era selezionata.

-- Dump della struttura di tabella BlazorFeste.arch_ordini_righe
CREATE TABLE IF NOT EXISTS `arch_ordini_righe` (
  `IdOrdine` bigint(20) NOT NULL,
  `IdRiga` int(11) NOT NULL,
  `IdCategoria` int(11) NOT NULL,
  `Categoria` varchar(255) NOT NULL,
  `IdProdotto` int(11) NOT NULL,
  `NomeProdotto` varchar(255) NOT NULL,
  `IdStatoRiga` int(11) NOT NULL,
  `QuantitàProdotto` int(11) NOT NULL,
  `QuantitàEvasa` int(11) NOT NULL DEFAULT 0,
  `Importo` double NOT NULL,
  `DataOra_RigaPresaInCarico` datetime NOT NULL,
  `DataOra_RigaEvasa` datetime NOT NULL,
  `QueueTicket` int(10) unsigned DEFAULT NULL COMMENT 'Valore del campo "consumoCumulativo" della tabella prodotti al momento dell''ordine',
  PRIMARY KEY (`IdOrdine`,`IdRiga`),
  CONSTRAINT `arch_ordini_righe_ibfk_1` FOREIGN KEY (`IdOrdine`) REFERENCES `arch_ordini` (`IdOrdine`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- L’esportazione dei dati non era selezionata.

-- Dump della struttura di trigger BlazorFeste.arch_ordini_before_insert
SET @OLDTMP_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_ZERO_IN_DATE,NO_ZERO_DATE,NO_ENGINE_SUBSTITUTION';
DELIMITER //
CREATE TRIGGER `arch_ordini_before_insert` BEFORE INSERT ON `arch_ordini` FOR EACH ROW BEGIN
--	DECLARE IdFestAttiva INT;
	DECLARE vDataAssegnazione datetime;
	DECLARE countOrdiniSerata INT;
  
	SELECT CASE 
		when HOUR(CURRENT_TIME) IN ( 8, 9,10,11,12,13,14,15,16)	then	DATE_ADD(CURRENT_DATE, interval 12 hour)
		when HOUR(CURRENT_TIME) IN (17,18,19,20,21,22,23) 			then 	DATE_ADD(CURRENT_DATE, interval 22 hour)
		ELSE 																				DATE_ADD(DATE_ADD(CURRENT_DATE, interval -1 day), interval 22 hour)
		END
	INTO vDataAssegnazione;

--	SET IdFestAttiva = ( SELECT IdFesta FROM arch_feste WHERE FestaAttiva = 1 ORDER BY DataFine DESC LIMIT 1);
	SET countOrdiniSerata = ( SELECT COUNT(*) FROM arch_ordini WHERE DataAssegnazione = vDataAssegnazione);

	SET 
--		NEW.IdFesta           = IdFestAttiva,
--		NEW.DataAssegnazione  = vDataAssegnazione, 
		NEW.ProgressivoSerata = countOrdiniSerata + 1;
END//
DELIMITER ;
SET SQL_MODE=@OLDTMP_SQL_MODE;

/*!40103 SET TIME_ZONE=IFNULL(@OLD_TIME_ZONE, 'system') */;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;
