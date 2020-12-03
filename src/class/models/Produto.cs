using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;


[Serializable]
class Produto
{
    
    public int CodigoProduto { get; set; }
    public string CodigoBarra { get; set; }
    public string NomeProduto { get; set; }

    public int CodigoGrupo { get; set; }

    public int CodigoSessao { get; set; }

    public string Formula { get; set; }

    public int Novo { get; set; }

    [NonSerialized]
    private PropertyInfo[] _PropertyInfos = null;

    public override string ToString()
    {
        if (_PropertyInfos == null)
            _PropertyInfos = this.GetType().GetProperties();
        
        var sb = new StringBuilder();

        foreach (var info in _PropertyInfos)
        {
            var value = info.GetValue(this, null) ?? "(null)";

            sb.Append(info.Name + ": " + value.ToString());
            sb.Append(" ");
        }
        return sb.ToString();
    }

    public bool Equals(Produto outro)
    {
        if (Object.ReferenceEquals(outro, null)) return false;
        if (Object.ReferenceEquals(this, outro)) return true;

        if (_PropertyInfos == null)
            _PropertyInfos = this.GetType().GetProperties();

        foreach (var info in _PropertyInfos)
        {
            var value = info.GetValue(this, null) ?? "(null)";
            var valueOther = info.GetValue(outro, null) ?? "(null)";

            if (!value.Equals(valueOther))
                return false;

        }
        return true;
    }

    public override int GetHashCode()
    {
        if (_PropertyInfos == null)
            _PropertyInfos = this.GetType().GetProperties();
        
        int hash = 0;

        foreach (var info in _PropertyInfos)
        {
            var value = info.GetValue(this, null) ?? "(null)";
            hash = hash ^ value.GetHashCode();
        }

        return hash;
    }

}