using DevToys.Api;

namespace DevToys.Web.Components.Pages;

partial class Home
{

}
public class WSMList : List<string>, IDataGridRow<string>
{
    public string? Details => "WSM";
}
