using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AerolineaVIP.Models.Dominio;

namespace AerolineaVIP.Models.ViewModels
{
    // ----- Módulo de counter (MOD-09) -----
    public class CounterViewModels
    {
        [Display(Name = "Código de reserva, documento o cliente")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Escriba entre {2} y {1} caracteres.")]
        public string Buscar { get; set; }

        public bool Buscado { get; set; }
        public List<FilaReservaVM> Reservas { get; set; }
        public List<vw_DisponibilidadVuelo> VuelosProximos { get; set; }

        public CounterViewModels()
        {
            Reservas = new List<FilaReservaVM>();
            VuelosProximos = new List<vw_DisponibilidadVuelo>();
        }
    }
}