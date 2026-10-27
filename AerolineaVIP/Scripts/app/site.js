/* =========================================================================
   Comportamientos comunes con jQuery (solo mejoran la experiencia; todas las
   reglas se validan también en el servidor).
   ========================================================================= */
(function ($) {
    'use strict';

    // Estado de carga: el botón se deshabilita y cambia su texto al enviar
    // (evita envíos repetidos, RNP-10).
    $(document).on('submit', 'form', function (e) {
        var $form = $(this);
        if ($.fn.valid && $form.data('validator') && !$form.valid()) { return; }
        var $btn = $(document.activeElement).is('button[type=submit], input[type=submit]') && $form.has(document.activeElement).length
            ? $(document.activeElement)
            : $form.find('button[type=submit][data-texto-carga]').first();
        if ($btn.length && $btn.data('texto-carga')) {
            // El botón se deshabilita después del envío para que su valor (p. ej. Accion=pagar) se incluya.
            $btn.data('texto-original', $btn.html());
            $btn.text($btn.data('texto-carga')).attr('aria-busy', 'true');
            setTimeout(function () { $btn.prop('disabled', true); }, 0);
        }
    });

    // Selector de destino dependiente del origen (pantallas 1 y 5).
    $(document).on('change', 'select[data-destinos-url]', function () {
        var $origen = $(this);
        var $destino = $($origen.data('destino'));
        var $boton = $($origen.data('boton'));
        $destino.prop('disabled', true).html('<option value="">Cargando…</option>');
        $.getJSON($origen.data('destinos-url'), { origen: $origen.val() })
            .done(function (lista) {
                $destino.empty();
                if (!lista.length) {
                    $destino.append('<option value="">Sin destinos disponibles</option>');
                    $boton.prop('disabled', true);
                    return;
                }
                $destino.append('<option value="">Seleccionar destino</option>');
                $.each(lista, function (i, d) { $destino.append($('<option>').val(d.codigo).text(d.texto)); });
                $destino.prop('disabled', false);
                $boton.prop('disabled', false);
            })
            .fail(function () {
                $destino.html('<option value="">No se pudieron cargar los destinos</option>');
            });
    });

    // Limpiar filtros sin recargar la página.
    $(document).on('click', '[data-limpiar]', function () {
        var $form = $($(this).data('limpiar'));
        $form.find('input[type=text], input[type=date], input[type=search]').val('');
        $form.find('input[type=number]').each(function () { $(this).val($(this).data('inicial') || ''); });
        $form.find('select').each(function () {
            var inicial = $(this).data('inicial');
            $(this).val(inicial !== undefined ? inicial : '').trigger('change');
        });
        $form.find('.input-validation-error').removeClass('input-validation-error');
        $form.find('.field-validation-error').empty();
    });

    // Mostrar / ocultar contraseña (pantalla 2).
    $(document).on('click', '[data-mostrar]', function () {
        var $campo = $($(this).data('mostrar'));
        var visible = $campo.attr('type') === 'text';
        $campo.attr('type', visible ? 'password' : 'text');
        $(this).text(visible ? 'Mostrar' : 'Ocultar').attr('aria-pressed', String(!visible));
    });

    // Solo dígitos en campos marcados.
    $(document).on('input', '[data-solo-digitos]', function () {
        var limpio = this.value.replace(/\D/g, '');
        if (limpio !== this.value) { this.value = limpio; }
    });

    // Fecha mínima = hoy en los buscadores.
    $(function () {
        var hoy = new Date();
        var iso = hoy.getFullYear() + '-' + ('0' + (hoy.getMonth() + 1)).slice(-2) + '-' + ('0' + hoy.getDate()).slice(-2);
        $('input[type=date][data-min-hoy]').attr('min', iso);
        // Lleva el foco al primer campo con error (17. datos inválidos y recuperación).
        var $error = $('.input-validation-error').first();
        if ($error.length) { $error.trigger('focus'); }
    });

    // Filtro de horario en resultados (pantalla 6).
    $(document).on('change', '#filtroHorario', function () {
        var franja = $(this).val();
        $('[data-franja]').each(function () {
            $(this).toggle(!franja || $(this).data('franja') === franja);
        });
    });
})(jQuery);
