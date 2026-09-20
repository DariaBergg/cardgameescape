using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class MerchantVisit
{
    protected MerchantData data;
    protected GameManager gm;
    protected MerchantUI ui;
    protected Action onLeave;

    public static void Start(MerchantData data, Action onLeave)
    {
        MerchantVisit visit;
        switch (data.kind)
        {
            case MerchantKind.Collector: visit = new CollectorVisit(); break;
            case MerchantKind.Blacksmith: visit = new BlacksmithVisit(); break;
            default: visit = new UsurerVisit(); break;
        }
        visit.data = data;
        visit.gm = GameManager.Instance;
        visit.ui = MerchantUI.Get();
        visit.ui.SetPanelOffset(null);
        visit.onLeave = onLeave;
        visit.Begin();
    }

    protected abstract void Begin();

    protected void Say(string speech, params MerchantUI.Option[] options)
    {
        var list = new List<MerchantUI.Option>(options);
        ui.Show(L.T(data.displayName), speech, list);
    }

    protected void Done(string speech)
    {
        Say(speech, Leave());
    }

    protected MerchantUI.Option Leave(string label = null)
    {
        return new MerchantUI.Option { label = label ?? L.T("Уйти"), action = () => { ui.Hide(); onLeave?.Invoke(); } };
    }

    protected MerchantUI.Option Opt(string label, string description, Action action, bool enabled = true)
    {
        return new MerchantUI.Option { label = label, description = description, action = action, enabled = enabled };
    }

    // Вариант с подсказкой при наведении (например, какие карты получишь)
    protected MerchantUI.Option OptWithTooltip(string label, string description, string tooltip, Action action, bool enabled = true)
    {
        return new MerchantUI.Option { label = label, description = description, tooltip = tooltip, action = action, enabled = enabled };
    }

    protected int Visits
    {
        get => int.TryParse(gm.Flag("Visits_" + data.Id), out int n) ? n : 0;
        set => gm.SetFlag("Visits_" + data.Id, value.ToString());
    }
}
