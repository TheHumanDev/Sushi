Hello World!
==================

.. important::
    Implementation for this example is still under development and a working Hello World is not yet supported.

Let's create our first Sushi program. In your project root, create a new file called ``.susproj`` and enter this code:

.. code-block:: toml
    :linenos:
    :caption: .susproj

    name = "Hello World!"
    assembly = "HelloWorld"

    language-version = "1.0"

    [build]
    default = "debug"
    sources = ["*.sus"]

    [build.targets.debug]
    type = "Sushi.Debug"

Then create a file called ``Program.sus`` and enter this code:

.. code-block:: sushi
    :linenos:
    :caption: Program.sus

    public root activity Program
    {
    }
