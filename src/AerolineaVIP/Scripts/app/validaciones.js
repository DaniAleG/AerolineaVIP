// Reglas extra para jQuery Validation.
(function ($) {
    'use strict';
    if (!$.validator || !$.validator.unobtrusive) { return; }

    // Casilla obligatoria (DebeSerVerdaderoAttribute).
    $.validator.addMethod('debeserverdadero', function (value, element) {
        return element.checked;
    });
    $.validator.unobtrusive.adapters.addBool('debeserverdadero');

    // Números con coma o punto decimal (cultura es-EC).
    $.validator.methods.number = function (value, element) {
        return this.optional(element) || /^-?(?:\d+|\d{1,3}(?:[.\s]\d{3})+)?(?:[.,]\d+)?$/.test(value);
    };
    $.validator.methods.range = function (value, element, param) {
        var n = parseFloat(String(value).replace(/\./g, '').replace(',', '.'));
        if (String(value).indexOf(',') === -1) { n = parseFloat(value); }
        return this.optional(element) || (n >= param[0] && n <= param[1]);
    };

    // Fechas ISO de los campos type="date".
    $.validator.methods.date = function (value, element) {
        return this.optional(element) || /^\d{4}-\d{2}-\d{2}$/.test(value) || !/Invalid|NaN/.test(new Date(value).toString());
    };

    // Cédula ecuatoriana: 10 dígitos cuando el tipo de documento es CEDULA.
    $.validator.addMethod('cedulaec', function (value, element) {
        var selector = $(element).data('tipo-documento');
        var tipo = selector ? $(selector).val() : 'CEDULA';
        if (tipo !== 'CEDULA') { return true; }
        return this.optional(element) || /^\d{10}$/.test(value);
    }, 'La cédula debe tener 10 dígitos.');

    // Asocia el mensaje de error a su campo.
    $.validator.setDefaults({
        highlight: function (element) {
            $(element).addClass('input-validation-error').attr('aria-invalid', 'true');
        },
        unhighlight: function (element) {
            $(element).removeClass('input-validation-error').removeAttr('aria-invalid');
        },
        focusInvalid: true
    });

    $(function () {
        $('.field-validation-valid, .field-validation-error').attr('role', 'alert');
        $('input[data-tipo-documento]').each(function () {
            $(this).rules('add', { cedulaec: true });
        });
    });
})(jQuery);
