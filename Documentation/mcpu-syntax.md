# MCPU Assembly syntax reference

This is the MCPU Assembly syntax reference. For the actual MCPU language reference, click [here](./language-reference.md).
```csharp

           mcpu_code := functions
                     +  main_token
                     +  line

                line := instruction
                     |  line [comment]ₒₚₜ
                     |  <empty_string>
                     |  line '\n' line

             comment := ';' <any_string>

         instruction := token
                     |  opcode [arguments]ₒₚₜ
                     |  jumplabel ':'

               token := '.kernel'
                     |  '.user'

           arguments := arguments arg_seperator arguments
                     |  argument

            argument := float_constant
                     |  int_constant
                     |  address
                     |  indirect_address
                     |  parameter

           parameter := '$' decimal_constant

      float_constant := signₒₚₜ float_core 'f'
                     |  signₒₚₜ float_core 'e' signₒₚₜ decimal_constant

          float_core := decimal_constant '.'ₒₚₜ
                     |  '.' decimal_constant

        int_constant := uint_constant
                     |  sign uint_constant

                sign := '+'
                     |  '-'

       uint_constant := decimal_constant
                     |  '0b' binary_constant
                     |  '0o' octal_constant
                     |  '0x' hexadecimal_constant
                     |  hexadecimal_constant 'h'

     binary_constant := '0'
                     |  '1'
                     |  binary_constant binary_constant

      octal_constant := binary_constant
                     |  '2'
                     |  '3'
                     |  '4'
                     |  '5'
                     |  '6'
                     |  '7'
                     |  octal_constant octal_constant

    decimal_constant := octal_constant
                     |  '8'
                     |  '9'
                     |  decimal_constant decimal_constant

hexadecimal_constant := binary_constant
                     |  'a'
                     |  'b'
                     |  'c'
                     |  'd'
                     |  'e'
                     |  'f'
                     |  hexadecimal_constant hexadecimal_constant

             address := 'k'ₒₚₜ '[' address_core ']'

    indirect_address := 'k'ₒₚₜ '[[' address_core ']]'

        address_core := uint_constant
                     |  parameter

       arg_seperator := ' '
                     |  ','
                     |  arg_seperator arg_seperator

          main_token := '.main'

           functions := function_declaration
                     |  functions '\n' functions

function_declaration := ['.inline']ₒₚₜ 'func' function_name [comment]ₒₚₜ
                     +  line
                     +  'end func' [comment]ₒₚₜ

```