﻿using dsstats.shared;
using dsstats.shared.Interfaces;

namespace dsstats.db8services;

public class RemoteToggleService : IRemoteToggleService
{
    public string _culture = "iv";
    public bool FromServer => true;

    public bool IsMaui => false;

    public string Culture => _culture;

#pragma warning disable CS0414 // dummy for server side, only useful for maui
    public event EventHandler? FromServerChanged = null;
    public event EventHandler? CultureChanged = null;

    public void Build(SpawnDto spawn, Commander commander, int team)
    {
        throw new NotImplementedException();
    }

    public void SetCulture(string culture)
    {
        _culture = culture;
    }
#pragma warning restore CS0414

    public void SetFromServer(bool fromServer)
    {

    }
}
