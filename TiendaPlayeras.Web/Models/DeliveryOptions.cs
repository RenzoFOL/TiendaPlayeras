using System.Collections.Generic;

namespace TiendaPlayeras.Web.Models
{
    public static class DeliveryOptions
    {
        public static List<DeliveryOption> All => new()
        {
            // Parque Juárez - Zona centro
            new DeliveryOption
            {
                Id = "PJ_MAR_1500",
                Point = "Parque Juárez - Zona centro",
                Schedule = "Martes 15:00 – 16:30"
            },
            new DeliveryOption
            {
                Id = "PJ_JUE_1300",
                Point = "Parque Juárez - Zona centro",
                Schedule = "Jueves 13:00 – 14:30"
            },
            new DeliveryOption
            {
                Id = "PJ_SAB_1100",
                Point = "Parque Juárez - Zona centro",
                Schedule = "Sábado 11:00 – 12:30"
            },

            // Plaza Ánimas
            new DeliveryOption
            {
                Id = "ANIM_MIE_1400",
                Point = "Plaza Ánimas",
                Schedule = "Miércoles 14:00 – 15:30"
            },
            new DeliveryOption
            {
                Id = "ANIM_SAB_1630",
                Point = "Plaza Ánimas",
                Schedule = "Sábado 16:30 – 18:00"
            },

            // Plaza Las Américas
            new DeliveryOption
            {
                Id = "AMER_MIE_1600",
                Point = "Plaza Las Américas",
                Schedule = "Miércoles 16:00 – 17:30"
            },
            new DeliveryOption
            {
                Id = "AMER_JUE_1600",
                Point = "Plaza Las Américas",
                Schedule = "Jueves 16:00 – 17:30"
            },

            // Museo de Antropología de Xalapa
            new DeliveryOption
            {
                Id = "MUS_MAR_1230",
                Point = "Museo de Antropología de Xalapa",
                Schedule = "Martes 12:30 – 14:00"
            },
            new DeliveryOption
            {
                Id = "MUS_SAB_1400",
                Point = "Museo de Antropología de Xalapa",
                Schedule = "Sábado 14:00 – 15:00"
            }
        };
    }
}
