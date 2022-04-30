﻿using Net.Leksi.Dto;

namespace Net.Leksi.RestContract;

internal class Requisitor
{
    private readonly DtoServiceProvider? _dtoServices;
    public Requisitor(IServiceProvider? services)
    {
        if (services is { })
        {
            _dtoServices = services.GetService<DtoServiceProvider>();
        }
        else
        {
            _dtoServices = null;
        }
    }


}