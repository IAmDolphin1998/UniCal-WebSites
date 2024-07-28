using Sitecore.ContentSearch;
using Sitecore.XA.Foundation.Search.Models;

namespace UniCal.Foundation.Search.Models
{
    public class DocumentModel : ContentPage
    {
        [IndexField("titolo")]
        public string Titolo { get; set; }
    }
}