using System;
using System.Collections.Generic;
using UnityEngine;

public interface IRegistry<T>
{
    public IReadOnlyList<T> Items{get;}
    public void Register(T item);
    public void Deregister(T item);
    

}
