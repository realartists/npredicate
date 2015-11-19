#!/bin/bash


echo "Compiling grammar $1"
java -jar ./antlr-4.5.1-complete.jar -Dlanguage=CSharp NSPredicate.g4
