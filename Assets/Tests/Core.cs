using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Core {
  public class Core : MonoBehaviour {
    TaskScope MainContext = new();
    void Start() {
    }
  }
}