(function ($) {
    "use strict";

    if (!$ || !$.validator || !$.validator.unobtrusive) return;

    $.validator.addMethod("commalist", function (value, element, maxLength) {
        if (this.optional(element)) return true;
        const entries = value.split(",").map(x => x.trim());
        if (entries.some(x => x.length === 0 || x.length > Number(maxLength))) return false;
        const normalized = entries.map(x => x.toLocaleLowerCase());
        return new Set(normalized).size === normalized.length;
    });

    $.validator.unobtrusive.adapters.addSingleVal("commalist", "maxlength");

    $(function () {
        $("form").each(function () {
            const form = $(this);
            form.removeData("validator");
            form.removeData("unobtrusiveValidation");
            $.validator.unobtrusive.parse(form);
        });
    });
})(window.jQuery);
