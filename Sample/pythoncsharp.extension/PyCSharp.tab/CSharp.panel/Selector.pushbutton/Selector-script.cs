#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion

namespace Selector.pushbutton
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            List<ElementId> selectedElementByRoom = new List<ElementId>();

            ISelectionFilter classFilter = new ClassISelectionFilter();
            Reference familyInstanceReference = uidoc.Selection.PickObject(ObjectType.Element, classFilter, "Select FamilyInstance");

            ISelectionFilter catFilterByName = new CategoryISelectionFilterByName("Rooms");
            Reference roomReference = uidoc.Selection.PickObject(ObjectType.Element, catFilterByName, "Select Room");
            
            ISelectionFilter catFilterByBuiltInCategory = new CategoryISelectionFilterByBuiltInCategory(BuiltInCategory.OST_Rooms);
            // Reference roomReference = uidoc.Selection.PickObject(ObjectType.Element, catFilterByBuiltInCategory, "Select Room");

            Element sourceFI = doc.GetElement(familyInstanceReference.ElementId);
            Element sourceRoom = doc.GetElement(roomReference.ElementId);

            List<FamilyInstance> selectedElements = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .WhereElementIsNotElementType()
                .Where(e => e.GetTypeId() == sourceFI.GetTypeId())
                .Cast<FamilyInstance>()
                .ToList();

            View view = uidoc.ActiveView;

            Transaction tx = new Transaction(doc, "Selected Element by CSharp");
            tx.Start();
            foreach (FamilyInstance fi in selectedElements)
            {
                if (fi.Room.Id == sourceRoom.Id)
                {
                    selectedElementByRoom.Add(fi.Id);
                    ((Element)fi).LookupParameter("Selected Comment").Set("Selected");
                }
                else
                {
                    ((Element)fi).LookupParameter("Selected Comment").Set("");
                }
            }
            tx.Commit();

            TaskDialog dialog = new TaskDialog("Selected Elements/Room");
            dialog.TitleAutoPrefix = false;
            dialog.MainInstruction = $"Room Name: {sourceRoom.Name} Room Number: {((SpatialElement)sourceRoom).Number}";
            dialog.MainContent = $"Family Name: {sourceFI.Name}\nType Name: {((FamilyInstance)sourceFI).Symbol.FamilyName}\nFamily Count: {selectedElementByRoom.Count}";
            dialog.Show();


            uidoc.Selection.SetElementIds(selectedElementByRoom);
            return Result.Succeeded;
        }
    }

    public class ClassISelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is FamilyInstance) return true;
            return false;
        }

        public bool AllowReference(Reference refer, XYZ pos)
        {
            return false;
        }
    }

    public class CategoryISelectionFilterByName : ISelectionFilter
    {
        string catName = String.Empty;

        public CategoryISelectionFilterByName(String selectedCatName)
        {
            catName = selectedCatName;
        }

        public bool AllowElement(Element elem)
        {
            if (elem.Category.Name == catName ) return true;
            return false;
        }

        public bool AllowReference(Reference refer, XYZ pos)
        {
            return false;
        }
    }

    public class CategoryISelectionFilterByBuiltInCategory : ISelectionFilter
    {
        BuiltInCategory BIC = BuiltInCategory.INVALID;

        public CategoryISelectionFilterByBuiltInCategory(BuiltInCategory bic)
        {
            BIC = bic;
        }

        public bool AllowElement(Element elem)
        {
            if (((BuiltInCategory)elem.Category.Id.IntegerValue) == BIC) return true;
            return false;
        }

        public bool AllowReference(Reference refer, XYZ pos)
        {
            return false;
        }
    }
}
