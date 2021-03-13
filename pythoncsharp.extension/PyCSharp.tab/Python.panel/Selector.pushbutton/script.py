# -*- coding: utf-8 -*-
from Autodesk.Revit import DB, UI
from rpw import db, ui
from System.Collections.Generic import List

uiapp = __revit__ # noqa F821
uidoc = uiapp.ActiveUIDocument
app = uiapp.Application
doc = uidoc.Document


class ClassISelectionFilter(UI.Selection.ISelectionFilter):
    def AllowElement(self, e):
        if isinstance(e, DB.FamilyInstance):
            return True
        else:
            return False

class CategoryISelectionFilterByName(UI.Selection.ISelectionFilter):
    def __init__(self, bi_cat):
        self.bi_cat = bi_cat

    def AllowElement(self, e):
        if e.Category.Name == self.bi_cat:
            return True
        else:
            return False

class CategoryISelectionFilterByBuiltInCategory(UI.Selection.ISelectionFilter):
    def __init__(self, bi_cat):
        self.bi_cat = bi_cat

    def AllowElement(self, e):
        if e.Category.Id.IntegerValue == int(self.bi_cat):
            return True
        else:
            return False


def main():

    selected_element_by_room = []

    family_instance_reference = uidoc.Selection.PickObject(
        UI.Selection.ObjectType.Element,
        ClassISelectionFilter(),
        "Select FamilyInstance")

    room_reference = uidoc.Selection.PickObject(
        UI.Selection.ObjectType.Element,
        CategoryISelectionFilterByName("Rooms"),
        "Select Room")

    """
    room_reference = uidoc.Selection.PickObject(
        UI.Selection.ObjectType.Element,
        CategoryISelectionFilterByBuiltInCategory(
            DB.BuiltInCategory.OST_Rooms),
        "Select Room")
    """

    source_fi = doc.GetElement(family_instance_reference.ElementId)
    source_room = doc.GetElement(room_reference.ElementId)
    # print(source_fi)

    selected_elements = db.Collector(
        of_class="FamilyInstance",
        is_not_type = True,
        where=lambda x: x.GetTypeId() == source_fi.GetTypeId()).get_elements()

    view = uidoc.ActiveView
    phase_name = view.get_Parameter(DB.BuiltInParameter.VIEW_PHASE).AsValueString()

    phase = get_phase(phase_name)

    with db.Transaction("Selected Element by Python"):
        for e in selected_elements:
            if e.Room[phase].Id == source_room.Id:
                selected_element_by_room.append(e)
                e.parameters['Selected Comment'].value = "Selected"
            else:
                e.parameters['Selected Comment'].value = ""

    ui.forms.Alert(
        "Family Name: {}\nType Name: {}\nFamily Count: {}".format(source_fi.Symbol.FamilyName, db.Element(source_fi).name, len(selected_element_by_room)), title="Selected Elements/Room", header="Room Name: {} Room Number: {}".format(db.Room(source_room).name, source_room.Number))

    uidoc.Selection.SetElementIds(List[DB.ElementId](e.Id for e in selected_element_by_room))

def get_phase(phase_name):
    phases = doc.Phases
    for p in phases:
        if p.Name == phase_name:
            return p

if __name__ == "__main__":
    main()
