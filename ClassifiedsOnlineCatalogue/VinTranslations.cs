using System.Collections.Generic;

namespace ClassifiedsOnlineCatalogue;

internal static class VinTranslations {
    private static Dictionary<string, string> translations = new Dictionary<string, string>()
    {
        { "Flexplate", "Placa flexível" },
        { "13 wheel", "Rodas aro 13" },
        { "14 wheel", "Rodas aro 14" },
        { "Water Pump Pulley", "Polia da bomba d'água" },
        { "Grille", "Grade" },
        { "Manual Gearbox 5 spd", "Câmbio manual 5 marchas" },
        { "Manual Gearbox 4 spd", "Câmbio manual 4 marchas" },
        { "Automatic Gearbox 3 spd", "Câmbio automático 3 marchas" },
        { "Brake Lines", "Cabos de freio" },
        { "Brake Master Cylinder", "Cilindro mestre de freio" },
        { "Heater Hose Inlet", "Mangueira de entrada do aquecedor" },
        { "Heater Hose Outlet", "Mangueira de saída do aquecedor" },
        { "Door", "Porta" },
        { "rightdoor", "Porta direita" },
        { "leftdoor", "Porta esquerda" },
        { "Passenger Seat", "Banco do passageiro" },
        { "Drive Seat", "Banco do motorista" },
        { "Gear Lever", "Alavanca do câmbio" },
        { "Column Shroud Left", "Capa esquerda da coluna de direção" },
        { "Column Shroud Right", "Capa direita da coluna de direção" },
        { "Rear Plate", "Placa traseira" },
        { "Front Upper Control Arm", "Braço de suspensão dianteiro superior" },
        { "Front Lower Control Arm", "braço de suspensão dianteiro inferior" },
        { "Rear Upper Control Arm", "Braço de suspensão traseiro superior" },
        { "Rear Lower Control Arm", "braço de suspensão traseiro inferior" },
        { "Timing Belt Cover", "Capa da correia dentada" },
        { "Gearbox Crossmember", "Suporte do câmbio" },
        { "Parcel Shelf", "Prateleira traseira" },
        { "Pedal Box Automatic", "Caixa de pedais automática" },
        { "Pedal Box Manual", "Caixa de pedais manual" },
        { "Rear Light", "Lanterna traseira" },
        { "Rear Light Left", "Lanterna traseira esquerda" },
        { "Rear Light Right", "Lanterna traseira direita" },
        { "Hood", "Capô" },
        { "Exhaust Muffler", "Silenciador do escapamento" },
        { "Bootlid", "Tampa do porta-malas" },
        { "Clutch Cable", "Cabo da embreagem" },
        { "Instrument Panel", "Painel de instrumentos" },
        { "Clutch Disc", "Disco de embreagem" },
        { "Steering Wheel", "Volante" },
        { "Head Light Assembly", "Conjunto do farol" },
        { "Head Light Assembly Left", "Conjunto do farol esquerdo" },
        { "Head Light Assembly Right", "Conjunto do farol direito" },
        { "Shifter Automatic", "Alavanca de câmbio automática" },
        { "Fuel tank", "Tanque de combustível" },
        { "Wiper Motor Assembly", "Motor do limpador" },
        { "Fresh Air Duct", "Duto de ar fresco" },
        { "Clutch Pressure Plate", "Placa de pressão da embreagem" },
        { "Thermostat", "Termostato" },
        { "Steering Shaft", "Eixo de direção" },
        { "Bumper", "Para-choque" },
        { "Driver Seat", "Banco do motorista" },
        { "Ventilation Box", "Caixa de ventilação" },
        { "Heater Box", "Caixa do aquecedor" },
        { "Timing Belt", "Correia dentada" },
        { "Rocker", "Balancim" },
        { "Flywheel", "Volante do motor" },
        { "Driveshaft", "Eixo de cardan" },
        { "Steering Rack", "Caixa de direção" },
        { "Hand Brake Lever", "Alavanca do freio de mão" },
        { "Radiator hose top", "Mangueira superior do radiador" },
        { "Radiator hose bottom", "Mangueira inferior do radiador" },
        { "Rear Axle", "Eixo traseiro" },
        { "Main Bearing", "Rolamento principal" },
        { "Alternator", "Alternador" },
        { "Center Console", "Console central" },
        { "Thermostat Housing", "Carcaça do termostato" },
        { "Crankshaft", "Virabrequim" },
        { "Front Left Brake Assembly", "Conjunto de freio dianteiro esquerdo" },
        { "Front Right Brake Assembly", "Conjunto de freio dianteiro direito" },
        { "Carburettor", "Carburador" },
        { "Headgasket", "Junta do cabeçote" },
        { "Dashboard", "Painel" },
        { "Dash Top Cover", "Capa superior do painel" },
        { "Dash Bottom Cover", "Capa inferior do painel" },
        { "Aux Shaft Sprocket", "Engrenagem do eixo auxiliar" },
        { "Front Link Right", "Braço dianteiro direito" },
        { "Front Link Left", "Braço dianteiro esquerdo" },
        { "Camshaft Standard", "Comando de válvulas Padrão" },
        { "Spring", "Mola" },
        { "Rear Seat Backrest", "Encosto do banco traseiro" },
        { "Rear Seat Bench", "Banco traseiro" },
        { "Fender", "Paralama" },
        { "Ignition Coil", "Bobina de ignição" },
        { "fan", "Ventoinha" },
        { "Starter", "Motor de partida" },
        { "Exhaust Pipe Front", "Tubo dianteiro do escapamento" },
        { "Exhaust Pipe Rear", "Tubo traseiro do escapamento" },
        { "Piston", "Pistão" },
        { "Fuel Pump", "Bomba de combustível" },
        { "Oil Pump", "Bomba de óleo" },
        { "Exhaust Manifold", "Coletor de escape" },
        { "Hubcap", "Calota" },
        { "Distributor", "Distribuidor" },
        { "Car Radio", "Rádio do carro" },
        { "Camshaft Sprocket", "Pinhão do comando de válvulas" },
        { "Front Shock Absorber", "Amortecedor dianteiro" },
        { "Rear Shock Absorber", "Amortecedor traseiro" },
        { "Air Cleaner", "Filtro de ar" },
        { "Oilpan", "Cárter" },
        { "Exhaust Headers", "Coletores de escape" },
        { "Steering Wheel gt", "Volante gt" },
        { "Aux Shaft", "Eixo auxiliar" },
        { "Rocker Cover", "Tampa de válvula" },
        { "Water Pump", "Bomba d'água" },
    };

    private static string NormalizeKey(string key) {
        if (string.IsNullOrEmpty(key)) return key;
        
        // Remove aspas smart/curvas (smart quotes)
        key = key.Replace("\u201C", "").Replace("\u201D", "");
        key = key.Replace("\u2018", "").Replace("\u2019", "");
        
        // Remove aspas duplas e simples
        key = key.Replace("\"", "").Replace("'", "");
        
        // Normaliza espaços múltiplos
        while (key.Contains("  ")) {
            key = key.Replace("  ", " ");
        }
        
        return key.Trim();
    }

    internal static string GetTranslation(string key) {
        if (string.IsNullOrEmpty(key)) return key;
        
        // Tenta correspondência exata primeiro
        if (translations.ContainsKey(key)) {
            return translations[key];
        }
        
        // Normaliza e tenta correspondência
        string normalized = NormalizeKey(key);
        foreach (var entry in translations) {
            if (NormalizeKey(entry.Key).Equals(normalized, System.StringComparison.OrdinalIgnoreCase)) {
                return entry.Value;
            }
        }
        
        return key; // Retorna a chave original se não encontrar tradução
    }

    internal static bool HasTranslation(string key) {
        return translations.ContainsKey(key);
    }
}
