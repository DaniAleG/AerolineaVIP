using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using AerolineaVIP.Infraestructura;

namespace AerolineaVIP
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Importes con coma o punto decimal (cultura es-EC).
            ModelBinders.Binders.Add(typeof(decimal), new DecimalModelBinder());
            ModelBinders.Binders.Add(typeof(decimal?), new DecimalModelBinder());
            MvcHandler.DisableMvcResponseHeader = true;

            // Creación por CÓDIGO de roles, superusuario, administrador y usuarios
            // iniciales, con la asignación de sus roles (ASP.NET Identity).
            InicializadorIdentity.Inicializar();
        }
    }
}
