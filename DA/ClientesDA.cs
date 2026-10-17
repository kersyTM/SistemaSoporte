using Abstracciones.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace DA
{
    public class ClientesDA : IClienteDA
    {
        private readonly List<int> _clientes = new() { 1, 2, 3, 4, 5 };
        public bool Existe(int id) => _clientes.Contains(id);
    }
}