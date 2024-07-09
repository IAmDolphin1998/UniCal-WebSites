using Sitecore;
using Sitecore.XA.Feature.Composites.Repositories;
using Sitecore.XA.Foundation.Mvc.Repositories.Base;
using System.Web.Script.Serialization;
using UniCal.Feature.Composites.Models;

namespace UniCal.Feature.Composites.Repositories
{
    public class PrimoPianoRepository : CompositeComponentRepository, IPrimoPianoRepository, IAbstractRepository<IRenderingModelBase>
    {
        //protected CarouselSettings Settings => this._settings ?? (this._settings = this.GetSettings());

        protected virtual string GetJsonProperties()
        {
            var data = new
            {
                timeout = 2000,
                isPauseEnabled = true,
                transition = "BasicTransition"
            };
            return new JavaScriptSerializer().Serialize((object)data);
        }

        //protected virtual CarouselSettings GetSettings()
        //{
        //    return new CarouselSettings()
        //    {
        //        NavigationType = this.Navigation,
        //        Timeout = this.IsEdit || this.Timeout <= 0 ? int.MaxValue : this.Timeout,
        //        PauseEnabled = MainUtil.GetBool(this.Rendering.Parameters["PauseOnHover"], false),
        //        Transition = this.Rendering.Parameters.GetEnumValue("Transition")
        //    };
        //}

        public override IRenderingModelBase GetModel()
        {
            PrimoPianoRenderingModel m = new PrimoPianoRenderingModel();
            this.FillBaseProperties((object)m);
            m.JsonDataProperties = this.GetJsonProperties();
            //m.Settings = this.GetSettings();
            return (IRenderingModelBase)m;
        }
    }
}