#!/bin/bash
cd $(pwd)
ROOTS=$(pants roots)
python3 -c "print('PYTHONPATH=./' + ':./'.join('''${ROOTS}'''.replace(' ', '\\ ').split('\n')) + ':\$PYTHONPATH')" > .env
pants export --py-resolve-format=symlinked_immutable_virtualenv --resolve=python-default
