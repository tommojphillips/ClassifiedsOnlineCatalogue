using System;
using System.Collections.Generic;

namespace ClassifiedsOnlineCatalogue
{
    internal static class VinTranslations
    {
        private static Dictionary<string, string> translations = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

        static VinTranslations()
        {
            // Adiciona entradas com capitalização normalizada
            translations.Add("Flexplate", "Placa flexível");
            translations.Add("13\" Wheel", "Rodas aro 13");
            translations.Add("14\" Wheel", "Rodas aro 14");
            translations.Add("Water Pump Pulley", "Polia da bomba d'água");
            translations.Add("Grille", "Grade");
            translations.Add("Manual Gearbox 5 spd", "Câmbio manual 5 marchas");
            translations.Add("Manual Gearbox 4 spd", "Câmbio manual 4 marchas");
            translations.Add("Automatic Gearbox 3 spd", "Câmbio automático 3 marchas");
            translations.Add("Brake Lines", "Cabos de freio");
            translations.Add("Brake Master Cylinder", "Cilindro mestre de freio");
            translations.Add("Heater Hose Inlet", "Mangueira de entrada do aquecedor");
            translations.Add("Heater Hose Outlet", "Mangueira de saída do aquecedor");
            translations.Add("Door", "Porta");
            translations.Add("Right Door", "Porta direita");
            translations.Add("Left Door", "Porta esquerda");
            translations.Add("Passenger Seat", "Banco do passageiro");
            translations.Add("Driver Seat", "Banco do motorista");
            translations.Add("Gear Lever", "Alavanca do câmbio");
            translations.Add("Column Shroud Left", "Capa esquerda da coluna de direção");
            translations.Add("Column Shroud Right", "Capa direita da coluna de direção");
            translations.Add("Rear Plate", "Placa traseira");
            translations.Add("Front Upper Control Arm", "Braço de suspensão dianteiro superior");
            translations.Add("Front Lower Control Arm", "Braço de suspensão dianteiro inferior");
            translations.Add("Rear Upper Control Arm", "Braço de suspensão traseiro superior");
            translations.Add("Rear Lower Control Arm", "Braço de suspensão traseiro inferior");
            translations.Add("Timing Belt Cover", "Capa da correia dentada");
            translations.Add("Gearbox Crossmember", "Suporte do câmbio");
            translations.Add("Parcel Shelf", "Prateleira traseira");
            translations.Add("Pedal Box Automatic", "Caixa de pedais automática");
            translations.Add("Pedal Box Manual", "Caixa de pedais manual");
            translations.Add("Rear Light", "Lanterna traseira");
            translations.Add("Rear Light Left", "Lanterna traseira esquerda");
            translations.Add("Rear Light Right", "Lanterna traseira direita");
            translations.Add("Hood", "Capô");
            translations.Add("Exhaust Muffler", "Silenciador do escapamento");
            translations.Add("Bootlid", "Tampa do porta-malas");
            translations.Add("Clutch Cable", "Cabo da embreagem");
            translations.Add("Instrument Panel", "Painel de instrumentos");
            translations.Add("Clutch Disc", "Disco de embreagem");
            translations.Add("Steering Wheel", "Volante");
            translations.Add("Head Light Assembly", "Conjunto do farol");
            translations.Add("Head Light Assembly Left", "Conjunto do farol esquerdo");
            translations.Add("Head Light Assembly Right", "Conjunto do farol direito");
            translations.Add("Shifter Automatic", "Alavanca de câmbio automática");
            translations.Add("Fuel Tank", "Tanque de combustível");
            translations.Add("Wiper Motor Assembly", "Motor do limpador");
            translations.Add("Fresh Air Duct", "Duto de ar fresco");
            translations.Add("Clutch Pressure Plate", "Placa de pressão da embreagem");
            translations.Add("Thermostat", "Termostato");
            translations.Add("Steering Shaft", "Eixo de direção");
            translations.Add("Bumper", "Para-choque");
            translations.Add("Ventilation Box", "Caixa de ventilação");
            translations.Add("Heater Box", "Caixa do aquecedor");
            translations.Add("Timing Belt", "Correia dentada");
            translations.Add("Rocker", "Balancim");
            translations.Add("Flywheel", "Volante do motor");
            translations.Add("Driveshaft", "Eixo cardan");
            translations.Add("Steering Rack", "Caixa de direção");
            translations.Add("Hand Brake Lever", "Alavanca do freio de mão");
            translations.Add("Radiator Hose Top", "Mangueira superior do radiador");
            translations.Add("Radiator Hose Bottom", "Mangueira inferior do radiador");
            translations.Add("Rear Axle", "Eixo traseiro");
            translations.Add("Main Bearing", "Mancal principal");
            translations.Add("Alternator", "Alternador");
            translations.Add("Center Console", "Console central");
            translations.Add("Thermostat Housing", "Carcaça do termostato");
            translations.Add("Crankshaft", "Virabrequim");
            translations.Add("Front Left Brake Assembly", "Conjunto de freio dianteiro esquerdo");
            translations.Add("Front Right Brake Assembly", "Conjunto de freio dianteiro direito");
            translations.Add("Carburettor", "Carburador");
            translations.Add("Headgasket", "Junta do cabeçote");
            translations.Add("Dashboard", "Painel");
            translations.Add("Dash Top Cover", "Capa superior do painel");
            translations.Add("Dash Bottom Cover", "Capa inferior do painel");
            translations.Add("Aux Shaft Sprocket", "Engrenagem do eixo auxiliar");
            translations.Add("Front Link Right", "Braço Dianteiro Direito");
            translations.Add("Front Link Left", "Braço Dianteiro Esquerdo");
            translations.Add("Camshaft Standard", "Comando de Válvulas Padrão");
            translations.Add("Spring", "Mola");
            translations.Add("Rear Seat Backrest", "Encosto do banco traseiro");
            translations.Add("Rear Seat Bench", "Banco traseiro");
            translations.Add("Fender", "Paralama");
            translations.Add("Ignition Coil", "Bobina de ignição");
            translations.Add("Fan", "Ventoinha");
            translations.Add("Starter", "Motor de partida");
            translations.Add("Exhaust Pipe Front", "Tubo dianteiro do escapamento");
            translations.Add("Exhaust Pipe Rear", "Tubo traseiro do escapamento");
            translations.Add("Piston", "Pistão");
            translations.Add("Fuel Pump", "Bomba de combustível");
            translations.Add("Oil Pump", "Bomba de óleo");
            translations.Add("Exhaust Manifold", "Coletor de escape");
            translations.Add("Hubcap", "Calota");
            translations.Add("Distributor", "Distribuidor");
            translations.Add("Car Radio", "Rádio do carro");
            translations.Add("Camshaft Sprocket", "Pinhão do comando de válvulas");
            translations.Add("Front Shock Absorber", "Amortecedor dianteiro");
            translations.Add("Rear Shock Absorber", "Amortecedor traseiro");
            translations.Add("Air Cleaner", "Filtro de ar");
            translations.Add("Oilpan", "Cárter");
            translations.Add("Exhaust Headers", "Coletores de escape");
            translations.Add("Steering Wheel GT", "Volante GT");
            translations.Add("Aux Shaft", "Eixo auxiliar");
            translations.Add("Rocker Cover", "Tampa de válvula");
            translations.Add("Water Pump", "Bomba d'água");
            translations.Add("Crankshaft Pulley", "Polia do virabrequim");
        }

        private static string NormalizeKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;
            return key.Trim();
        }

        internal static string GetTranslation(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;
            
            string normalized = NormalizeKey(key);
            
            // Tenta correspondência exata
            if (translations.ContainsKey(normalized))
            {
                return translations[normalized];
            }
            
            return key; // Retorna a chave original se não encontrar tradução
        }
    }
}
