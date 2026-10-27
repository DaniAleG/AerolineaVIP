/* =========================================================================
   Pantalla 11 · Pago SIMULADO.
   - Solo números en número, vencimiento y código (keypress/input).
   - maxlength dinámico según la marca (LongitudNumero / LongitudCodigoSeguridad).
   - Vencimiento MM/AA no pasado.
   - "Pagar" valida y abre el modal; el POST real sale solo desde el botón
     «Pagar» del modal. «Cancelar» cierra el modal sin registrar ningún pago.
   ========================================================================= */
(function ($) {
    'use strict';
    var $form = $('#formPago');
    if (!$form.length) { return; }

    var $numero = $('#NumeroTarjeta');
    var $codigo = $('#CodigoSeguridad');
    var $venc = $('#Vencimiento');

    function marca() {
        var $m = $('input[name="MarcaTarjetaCodigo"]:checked');
        return $m.length ? {
            codigo: $m.val(),
            nombre: $m.data('nombre'),
            largo: parseInt($m.data('largo'), 10),
            cvv: parseInt($m.data('cvv'), 10)
        } : null;
    }

    function digitos(v) { return (v || '').replace(/\D/g, ''); }

    function formatearNumero() {
        var m = marca();
        var d = digitos($numero.val());
        if (m) { d = d.substring(0, m.largo); }
        $numero.val(d.replace(/(\d{4})(?=\d)/g, '$1 '));
    }

    function aplicarMarca() {
        var m = marca();
        if (!m) { return; }
        $numero.attr('maxlength', m.largo + Math.floor((m.largo - 1) / 4));
        $codigo.attr('maxlength', m.cvv);
        $('#ayudaNumero').text('Solo números · ' + m.largo + ' dígitos para ' + m.nombre);
        $('#ayudaCodigo').text(m.cvv + ' dígitos' + (m.cvv === 4 ? ' (American Express)' : ''));
        formatearNumero();
        if ($codigo.val().length > m.cvv) { $codigo.val($codigo.val().substring(0, m.cvv)); }
    }

    // Solo dígitos al escribir.
    $numero.add($codigo).add($venc).on('keypress', function (e) {
        if (e.which && (e.which < 48 || e.which > 57)) { e.preventDefault(); }
    });
    $numero.on('input', formatearNumero);
    $codigo.on('input', function () { this.value = digitos(this.value); });
    $venc.on('input', function () {
        var d = digitos(this.value).substring(0, 4);
        this.value = d.length > 2 ? d.substring(0, 2) + '/' + d.substring(2) : d;
    });
    $('input[name="MarcaTarjetaCodigo"]').on('change', aplicarMarca);

    // Reglas adicionales del cliente (el servidor las repite).
    $.validator.addMethod('largomarca', function (value) {
        var m = marca();
        return !m || digitos(value).length === m.largo;
    }, function () {
        var m = marca();
        return m ? 'El número debe tener ' + m.largo + ' dígitos para ' + m.nombre + '.' : 'Número inválido.';
    });
    $.validator.addMethod('cvvmarca', function (value) {
        var m = marca();
        return !m || digitos(value).length === m.cvv;
    }, function () {
        var m = marca();
        return m ? 'El código de seguridad tiene ' + m.cvv + ' dígitos para ' + m.nombre + '.' : 'Código inválido.';
    });
    $.validator.addMethod('vencimientovigente', function (value) {
        var m = /^(0[1-9]|1[0-2])\/(\d{2})$/.exec(value);
        if (!m) { return false; }
        var fin = new Date(2000 + parseInt(m[2], 10), parseInt(m[1], 10), 0, 23, 59, 59);
        return fin >= new Date();
    }, 'La tarjeta está vencida o el formato no es MM/AA.');

    $(function () {
        $numero.rules('add', { largomarca: true });
        $codigo.rules('add', { cvvmarca: true });
        $venc.rules('add', { vencimientovigente: true });
        aplicarMarca();
    });

    // Botón "Pagar": valida y abre el modal de confirmación.
    $('#btnAbrirModal').on('click', function () {
        if (!$form.valid()) { return; }
        var m = marca();
        $('#modalMetodo').text($('input[name="MetodoPagoCodigo"]:checked').data('nombre') + ' · ' + (m ? m.nombre : '') + ' **** ' + digitos($numero.val()).slice(-4));
        var modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('modalConfirmarPago'));
        modal.show();
    });

    // Modal · Pagar: envío real al servidor.
    $('#btnConfirmarPago').on('click', function () {
        $(this).prop('disabled', true).text('Procesando pago simulado…');
        $('#btnCancelarModal').prop('disabled', true);
        $form.off('submit.validate');
        $form[0].submit();
    });

    // Enter en el formulario abre el modal en vez de enviar.
    $form.on('submit', function (e) {
        if (!$form.data('confirmado')) {
            e.preventDefault();
            $('#btnAbrirModal').trigger('click');
        }
    });
    $('#btnConfirmarPago').on('click', function () { $form.data('confirmado', true); });
})(jQuery);
