using System.Web.Optimization;

namespace AerolineaVIP
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            // jQuery Validation + adaptador unobtrusive + reglas propias (site.js)
            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate.js",
                        "~/Scripts/jquery.validate.unobtrusive.js",
                        "~/Scripts/app/validaciones.js"));

            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            // Bootstrap 5 con Popper incluido (modales y menús desplegables).
            bundles.Add(new Bundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.bundle.js"));

            bundles.Add(new ScriptBundle("~/bundles/app").Include(
                      "~/Scripts/app/site.js"));

            bundles.Add(new ScriptBundle("~/bundles/asientos").Include(
                      "~/Scripts/app/asientos.js"));

            bundles.Add(new ScriptBundle("~/bundles/pago").Include(
                      "~/Scripts/app/pago.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/Site.css",
                      "~/Content/asientos.css"));
        }
    }
}
