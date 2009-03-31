#include <stdio.h>

typedef void* (*FuncPtr)(int, float);

namespace Foo
{
  FuncPtr foo;
  FuncPtr bar;
}

void tt()
{
  Foo::foo(5, 5.0);
  Foo::bar(5, 5.0);
}
