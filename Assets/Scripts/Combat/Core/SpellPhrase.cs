public struct SpellPhrase
{
    public ModifierWord? Modifier { get; private set; }
    public ElementWord? Element { get; private set; }
    public FormWord? Form { get; private set; }

    public bool IsComplete => Modifier.HasValue && Element.HasValue && Form.HasValue;

    public void SetModifier(ModifierWord modifier)
    {
        Modifier = modifier;
    }

    public void SetElement(ElementWord element)
    {
        Element = element;
    }

    public void SetForm(FormWord form)
    {
        Form = form;
    }

    public void Clear()
    {
        Modifier = null;
        Element = null;
        Form = null;
    }

    public override string ToString()
    {
        var modifierLabel = Modifier.HasValue ? CombatWordDefinitions.GetLabel(Modifier.Value) : "--";
        var elementLabel = Element.HasValue ? CombatWordDefinitions.GetLabel(Element.Value) : "--";
        var formLabel = Form.HasValue ? CombatWordDefinitions.GetLabel(Form.Value) : "--";

        return $"{modifierLabel} - {elementLabel} - {formLabel}";
    }
}
