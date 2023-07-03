"use strict";

export var ServizioObj = {
  objRef: null,
  ToolBar: null,

  init: (_objRef, _casse) => {
    console.log(moment().format("HH:mm:ss.SSS") + " - " + " ServizioObj - init");

    ServizioObj.objRef = _objRef;

    let _items = [
      {
        location: 'before',
        widget: 'dxButton',
        options: {
          icon: 'fa-solid fa-cash-register',
          type: 'default',
          text: 'Lista 1',
          hint: "Pagina Gestione Lista",
          onClick() {
            _objRef.invokeMethodAsync("NavigateToPage", "GestioneLista/1")
              .catch(err => console.error(err.toString()));
          },
        },
      }
    ];

    _casse.forEach(_cassa => {
      if (_cassa.abilitata && (!_cassa.visibile || !_cassa.scontrinoAbilitato)) { 
        _items.push(
          {
            location: 'before',
            widget: 'dxButton',
            options: {
              icon: 'fa-solid fa-cash-register',
              type: 'default',
              text: _cassa.cassa,
              hint: "Pagina Gestione Cassa",
              onClick() {
                _objRef.invokeMethodAsync("NavigateToPage", "GestioneCassa/" + _cassa.idCassa)
                  .catch(err => console.error(err.toString()));
              },
            },
          }
        )
      }
    });

    _items.push(
      //{
      //  location: 'before',
      //  widget: 'dxButton',
      //  options: {
      //    icon: 'fa-solid fa-cash-register',
      //    type: 'default',
      //    text: 'Stato Prodotto 29',
      //    hint: "Pagina Stato Prodotto",
      //    onClick() {
      //      _objRef.invokeMethodAsync("NavigateToPage", "StatoProdotto/29")
      //        .catch(err => console.error(err.toString()));
      //    },
      //  },
      //},
      {
        location: 'before',
        widget: 'dxButton',
        options: {
          icon: 'fa-solid fa-dolly',
          type: 'default',
          text: 'Reload Anagrafiche',
          hint: "Reload Anagrafiche",
          onClick() {
            _objRef.invokeMethodAsync('RefreshAnagrafiche')
              .catch(err => console.error(err.toString()));
          },
        },
      }
    );

    $("#ServizioToolbar").dxToolbar({
      onInitialized(e) {
        ServizioObj.ToolBar = e.component;
      },
      items: _items,
    });
  },

  dispose: () => {
    try {
      $("#ServizioToolbar").dxToolbar("dispose");
      $("#ServizioToolbar").remove();

      console.log(moment().format("HH:mm:ss.SSS") + " - " + " ServizioObj - dispose");
    } catch (error) {
      console.error(moment().format("HH:mm:ss.SSS") + " - " + " ServizioObj - dispose - Error: " + error);
    }
  },
};