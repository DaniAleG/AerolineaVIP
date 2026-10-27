/* =========================================================================
   Mapa de asientos (pantallas 7 y 8).
   - Los colores vienen del servidor (vw_EstadoAsientoVuelo).
   - Solo los asientos verdes son seleccionables; rojos y morados están
     deshabilitados (disabled + aria-disabled).
   - La selección es TEMPORAL y se guarda en campos ocultos asiento_{p}_{t};
     el servidor la revalida al continuar y al confirmar (RNP-04).
   ========================================================================= */
(function ($) {
    'use strict';

    var $form = $('#formAsientos');
    var anunciar = function (texto) { $('#anuncio').text(texto); };

    // ---------- Pantalla 7 y 8: "Actualizar disponibilidad" ----------
    function aplicarEstados($mapa, datos) {
        $.each(datos.asientos, function (i, a) {
            var $s = $mapa.find('.asiento[data-numero="' + a.numero + '"]');
            var seleccionado = $s.hasClass('asiento-seleccionado');
            $s.removeClass('asiento-disponible asiento-reservado asiento-ocupado').addClass(a.clase)
              .attr('data-estado', a.estado).attr('aria-label', a.descripcion);
            $s.find('.estado-texto').text(a.texto);
            if ($s.is('button')) {
                var habilitado = a.estado === 'DISPONIBLE';
                $s.prop('disabled', !habilitado).attr('aria-disabled', String(!habilitado));
                if (!habilitado && seleccionado) {
                    quitarAsiento($s.data('tramo'), a.numero);
                    $('#mensajesMapa').html('<div class="alert alert-warning" role="alert"><strong>Advertencia:</strong> El asiento ' + a.codigo + ' acaba de ser ocupado. Seleccione otro.</div>');
                }
            }
        });
    }

    $(document).on('click', '[data-actualizar]', function () {
        var $btn = $(this);
        var $mapa = $($btn.data('mapa'));
        var textoOriginal = $btn.text();
        $btn.prop('disabled', true).text('Actualizando disponibilidad…');
        $mapa.addClass('mapa-cargando');
        $.getJSON($btn.data('actualizar'))
            .done(function (datos) {
                aplicarEstados($mapa, datos);
                var tramo = $mapa.data('tramo') || 0;
                $('[data-resumen-disponibles="' + tramo + '"]').text(datos.disponibles);
                $('[data-resumen-reservados="' + tramo + '"]').text(datos.reservados);
                $('[data-resumen-ocupados="' + tramo + '"]').text(datos.ocupados);
                $('[data-corte]').text(datos.corte);
                anunciar('Disponibilidad actualizada a las ' + datos.corte + '.');
            })
            .always(function () {
                $btn.prop('disabled', false).text(textoOriginal);
                $mapa.removeClass('mapa-cargando');
            });
    });

    if (!$form.length) { return; }

    // ---------- Pantalla 8: selección ----------
    var pasajeros = parseInt($form.data('pasajeros'), 10);
    var tramos = parseInt($form.data('tramos'), 10);
    var activo = 0;          // pasajero activo
    var tramoActivo = 0;

    function campo(p, t) { return $('#asiento_' + p + '_' + t); }

    function codigoDe(t, numero) {
        return $('.mapa[data-tramo="' + t + '"] .asiento[data-numero="' + numero + '"]').data('codigo');
    }

    function quitarAsiento(t, numero) {
        for (var p = 0; p < pasajeros; p++) {
            if (parseInt(campo(p, t).val(), 10) === numero) { campo(p, t).val('0'); }
        }
        refrescar();
    }

    function refrescar() {
        // Marca en cada mapa los asientos elegidos y quién los tiene.
        $('.mapa .asiento').removeClass('asiento-seleccionado').attr('aria-pressed', 'false').find('.pasajero-tag').remove();
        var completos = true;
        for (var p = 0; p < pasajeros; p++) {
            for (var t = 0; t < tramos; t++) {
                var n = parseInt(campo(p, t).val(), 10) || 0;
                var $celda = $('#resumen_' + p + '_' + t);
                if (n > 0) {
                    var $s = $('.mapa[data-tramo="' + t + '"] .asiento[data-numero="' + n + '"]');
                    $s.addClass('asiento-seleccionado').attr('aria-pressed', 'true')
                      .append('<span class="pasajero-tag">P' + (p + 1) + '</span>');
                    $celda.text(codigoDe(t, n) + ' · ' + ($s.data('ubicacion') || ''));
                } else {
                    $celda.text('—');
                    completos = false;
                }
            }
        }
        $('[data-pasajero]').removeClass('table-active').attr('aria-current', null);
        $('[data-pasajero="' + activo + '"]').addClass('table-active').attr('aria-current', 'true');
        $('#pasajeroActivoTexto').text('Pasajero ' + (activo + 1));
        $('#btnContinuar').prop('disabled', !completos);
        $('#faltantes').toggle(!completos);
    }

    function siguientePendiente() {
        for (var p = 0; p < pasajeros; p++) {
            if (!(parseInt(campo(p, tramoActivo).val(), 10) > 0)) { return p; }
        }
        return activo;
    }

    $(document).on('click', '.mapa button.asiento', function () {
        var $s = $(this);
        if ($s.prop('disabled') || $s.data('estado') !== 'DISPONIBLE') { return; }
        var t = parseInt($s.closest('.mapa').data('tramo'), 10);
        var n = parseInt($s.data('numero'), 10);
        tramoActivo = t;

        // ¿Ya lo tiene otro pasajero en este tramo? Se lo quita (un asiento por pasajero y tramo).
        for (var p = 0; p < pasajeros; p++) {
            if (parseInt(campo(p, t).val(), 10) === n) {
                campo(p, t).val('0');
                if (p === activo) { refrescar(); anunciar('Asiento ' + $s.data('codigo') + ' liberado para el Pasajero ' + (p + 1) + '.'); return; }
            }
        }
        campo(activo, t).val(n);
        anunciar('Asiento ' + $s.data('codigo') + ' seleccionado para el Pasajero ' + (activo + 1) + '.');
        activo = siguientePendiente();
        refrescar();
    });

    $(document).on('click', '[data-pasajero]', function () {
        activo = parseInt($(this).data('pasajero'), 10);
        refrescar();
        anunciar('Pasajero ' + (activo + 1) + ' activo.');
    });

    $(document).on('shown.bs.tab', '[data-tramo-tab]', function () {
        tramoActivo = parseInt($(this).data('tramo-tab'), 10);
    });

    $('#btnQuitar').on('click', function () {
        campo(activo, tramoActivo).val('0');
        refrescar();
        anunciar('Selección del Pasajero ' + (activo + 1) + ' quitada.');
    });

    $form.on('submit', function () {
        $('#btnContinuar').text('Comprobando disponibilidad…').attr('aria-busy', 'true');
    });

    refrescar();
})(jQuery);
