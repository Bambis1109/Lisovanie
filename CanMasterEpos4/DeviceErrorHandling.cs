namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class DeviceErrorHandling : CCommandGroupCO
                {
                    public DeviceErrorHandling(ushort keyHandle, byte nodeId, CDataCO Data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        this.Data = Data;
                    }

                    public ushort GetDeviceErrorCode(byte deviceErrorNumber)
                    {
                        // Kontrola rozsahu subindexov pre Error History (0x01 až 0x05)
                        // Podľa dokumentácie je max. podporovaný subindex 5
                        if (deviceErrorNumber < 1 || deviceErrorNumber > 5)
                        {
                            throw new CDeviceException(
                                $"Node:{NodeId}, Error history index {deviceErrorNumber} je mimo povolený rozsah (1-5).");
                        }

                        // Čítanie 4 bajtov (UNSIGNED32) a extrakcia 16-bitového kódu chyby
                        // Objekt 0x1003, subindex 0x01..0x05
                        // Použitie masky 0xFFFF odfiltruje stavové bity a vráti čistý kód chyby
                        return (ushort)(ReadSdo(0x1003, deviceErrorNumber, 4) & 0xFFFF);
                    }

                    public byte GetNbOfDeviceError()
                    {
                        // Čítanie 1 bajtu (UNSIGNED8) - počet aktuálnych chýb v histórii
                        // Objekt 0x1003, subindex 0x00
                        return (byte)ReadSdo(0x1003, 0x00, 1);
                    }

                    public string GetErrorDescription(ushort errorCode)
                    {
                        switch (errorCode)
                        {
                            case 0x0000: return "No Error (Žiadna chyba)";
                            case 0x1000: return "Generic error (Všeobecná chyba)";
                            case 0x1080:
                            case 0x1081:
                            case 0x1082:
                            case 0x1083:
                            case 0x1084:
                            case 0x1085:
                            case 0x1086:
                            case 0x1087:
                            case 0x1088: return "Generic initialization error (Chyba inicializácie)";
                            case 0x1090: return "Firmware incompatibility error (Nekompatibilita firmvéru)";
                            case 0x2310: return "Overcurrent error (Nadprúd)";
                            case 0x2320: return "Power stage protection error (Ochrana výkonového stupňa)";
                            case 0x3210: return "Overvoltage error (Prepätie)";
                            case 0x3220: return "Undervoltage error (Podpätie)";
                            case 0x4210: return "Thermal overload error (Tepelné preťaženie jednotky)";
                            case 0x4380: return "Thermal motor overload error (Tepelné preťaženie motora)";
                            case 0x5113: return "Logic supply voltage too low error (Nízke napätie logiky)";
                            case 0x5280: return "Hardware defect error (Hardvérový defekt)";
                            case 0x5281: return "Hardware incompatibility error (Hardvérová nekompatibilita)";
                            case 0x5282: return "STO card detection error (Chyba detekcie STO karty)";
                            case 0x5480:
                            case 0x5481:
                            case 0x5482:
                            case 0x5483: return "Hardware error (Hardvérová chyba)";
                            case 0x6080: return "Sign of life error (Chyba Sign of life)";
                            case 0x6081: return "Extension 1 watchdog error (Watchdog rozšírenia 1)";
                            case 0x6320: return "Software parameter error (Chyba softvérových parametrov)";
                            case 0x6380:
                                return "Persistent parameter corrupt error (Korupcia perzistentných parametrov)";
                            case 0x7320: return "Position sensor error (Chyba snímača polohy)";
                            case 0x7380:
                                return "Position sensor breach error (Porušenie snímača polohy - Position Clear!)";
                            case 0x7381:
                                return "Position sensor resolution error (Chyba rozlíšenia snímača - Position Clear!)";
                            case 0x7382: return "Position sensor index error (Chyba indexu snímača - Position Clear!)";
                            case 0x7388: return "Hall sensor error (Chyba Hallových sond - Position Clear!)";
                            case 0x7389:
                                return "Hall sensor not found error (Hallove sondy nenájdené - Position Clear!)";
                            case 0x738A:
                                return "Hall angle detection error (Chyba detekcie uhla Hall - Position Clear!)";
                            case 0x738C: return "SSI sensor error (Chyba SSI snímača)";
                            case 0x738D: return "SSI sensor frame error (Chyba rámca SSI)";
                            case 0x7390: return "Missing main sensor error (Chýbajúci hlavný snímač)";
                            case 0x7391: return "Missing commutation sensor error (Chýbajúci komutačný snímač)";
                            case 0x7392:
                                return "Main sensor direction error (Chyba smeru hlavného snímača - Position Clear!)";
                            case 0x8110: return "CAN overrun error - object lost (CAN overrun - strata objektu)";
                            case 0x8111: return "CAN overrun error (CAN overrun)";
                            case 0x8120: return "CAN passive mode error (CAN passive mode)";
                            case 0x8130: return "CAN heartbeat error (Chyba CAN heartbeat)";
                            case 0x8150: return "CAN PDO COB-ID collision (Kolízia COB-ID)";
                            case 0x81FD: return "CAN bus turned off (CAN bus vypnutý)";
                            case 0x81FE: return "CAN Rx queue overflow (Pretečenie Rx fronty)";
                            case 0x81FF: return "CAN Tx queue overflow (Pretečenie Tx fronty)";
                            case 0x8210: return "CAN PDO length error (Chyba dĺžky PDO)";
                            case 0x8250: return "RPDO timeout";
                            case 0x8611: return "Following error (Vlečná chyba)";
                            case 0x8A80: return "Negative limit switch error (Chyba záporného koncového spínača)";
                            case 0x8A81: return "Positive limit switch error (Chyba kladného koncového spínača)";
                            case 0x8A82: return "Software position limit error (Chyba softvérového limitu polohy)";
                            case 0x8A88: return "STO error";
                            case 0x8A8A: return "STO card ready warning (Varovanie STO karty)";
                            case 0x8A8B: return "STO card inactive state error (Chyba neaktívneho stavu STO)";
                            case 0xFF01: return "System overloaded error (Systém preťažený)";
                            case 0xFF02: return "Watchdog error (Kritická chyba watchdogu - Position Clear!)";
                            case 0xFF0B: return "System peak overloaded error (Špičkové preťaženie systému)";
                            case 0xFF10: return "Controller gain error (Chyba zosilnenia regulátora)";
                            default:
                                if (errorCode >= 0x6180 && errorCode <= 0x61F0)
                                    return "Internal software error (Interná softvérová chyba)";
                                if (errorCode >= 0xFF11 && errorCode <= 0xFF24)
                                    return "Auto tuning error (Chyba auto-tuningu)";
                                return $"Unknown Error Code: 0x{errorCode:X4}";
                        }
                    }
                }
            }
        }
    }
}