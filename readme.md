# Epinova.Associations

Easy to use two way relationship between content.

- Supports both `ContentArea` and `IList<ContentReference>` as both source and target for an association, and you can use differerent types in each end of the relationship.
- Matches association sources and targets on **property name**
- To use, let your content type class implement IAssociationContent, and simply add `[ContentAssociation]` to the properties you want to use for associations.
- Associations are saved to the other side on publishing

## Examples

    public class EmployeePage : BasePageData, IAssociationContent
    {
        [ContentAssociation]
        public virtual ContentArea EmployeeDocuments { get; set; }
    }

    public class DocumentPage : BasePageData, IAssociationContent
    {
        [ContentAssociation]
        public virtual IList<ContentReference> EmployeeDocuments { get; set; }
    }

Here, whatever you add into the `EmployeeDocuments` will be reflected to the other side of the relationship when you publish your content.

You can even use `[AllowedTypes]` in combination with this to restrict what types go where, like so:

    public class EmployeePage : BasePageData, IAssociationContent
    {
        [ContentAssociation]
        [AllowedTypes(typeof(DocumentPage))]
        public virtual ContentArea EmployeeDocuments { get; set; }
    }

    public class DocumentPage : BasePageData, IAssociationContent
    {
        [ContentAssociation]
        [AllowedTypes(typeof(EmpolyeePage))]
        public virtual IList<ContentReference> EmployeeDocuments { get; set; }
    }
