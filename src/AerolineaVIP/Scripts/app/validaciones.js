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

    // Cédula ecuatoriana: provincia, tercer dígito y dígito verificador (módulo 10).
    function cedulaValida(cedula) {
        if (!/^\d{10}$/.test(cedula)) { return false; }
        var provincia = parseInt(cedula.substring(0, 2), 10);
        if ((provincia < 1 || provincia > 24) && provincia !== 30) { return false; }
        if (parseInt(cedula.charAt(2), 10) >= 6) { return false; }

        var suma = 0;
        for (var i = 0; i < 9; i++) {
            var digito = parseInt(cedula.charAt(i), 10);
            if (i % 2 === 0) {
                digito *= 2;
                if (digito > 9) { digito -= 9; }
            }
            suma += digito;
        }
        var calculado = (10 - suma % 10) % 10;
        return calculado === parseInt(cedula.charAt(9), 10);
    }

    // RUC: persona natural (cédula + 001), sociedad pública (6) o privada (9) con módulo 11.
    function modulo11(ruc, coeficientes) {
        var suma = 0;
        for (var i = 0; i < coeficientes.length; i++) {
            suma += parseInt(ruc.charAt(i), 10) * coeficientes[i];
        }
        var calculado = 11 - suma % 11;
        if (calculado === 11) { calculado = 0; }
        if (calculado === 10) { return false; }
        return calculado === parseInt(ruc.charAt(coeficientes.length), 10);
    }

    function rucValido(ruc) {
        if (!/^\d{13}$/.test(ruc)) { return false; }
        var provincia = parseInt(ruc.substring(0, 2), 10);
        if ((provincia < 1 || provincia > 24) && provincia !== 30) { return false; }
        var tercero = parseInt(ruc.charAt(2), 10);
        if (tercero < 6) { return cedulaValida(ruc.substring(0, 10)) && ruc.substring(10) !== '000'; }
        if (tercero === 6) { return modulo11(ruc, [3, 2, 7, 6, 5, 4, 3, 2]) && ruc.substring(9) !== '0000'; }
        if (tercero === 9) { return modulo11(ruc, [4, 3, 2, 7, 6, 5, 4, 3, 2]) && ruc.substring(10) !== '000'; }
        return false;
    }

    // Cédula: solo se valida cuando el tipo de documento elegido es CEDULA.
    $.validator.addMethod('cedulaec', function (value, element) {
        var selector = $(element).data('tipo-documento');
        var tipo = selector ? $(selector).val() : 'CEDULA';
        if (tipo !== 'CEDULA') { return true; }
        return this.optional(element) || cedulaValida(value);
    }, 'La cédula no es válida (10 dígitos, revise el número).');

    // RUC (RucAttribute).
    $.validator.addMethod('rucec', function (value, element) {
        return this.optional(element) || rucValido(value);
    });
    $.validator.unobtrusive.adapters.addBool('rucec');

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
        // Al cambiar el tipo de documento se vuelve a validar el número.
        $(document).on('change', 'select', function () {
            var $doc = $('input[data-tipo-documento="#' + this.id + '"]');
            if ($doc.val()) { $doc.valid(); }
        });
    });
})(jQuery);
