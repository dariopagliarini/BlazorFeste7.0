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
          hint: "Pagina Gestione Lista 1",
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
            _objRef.invokeMethodAsync('RefreshAnagrafiche', 0)
              .catch(err => console.error(err.toString()));
          },
        },
      },
      {
        location: 'before',
        widget: 'dxButton',
        options: {
          icon: 'fa-solid fa-circle-xmark',
          type: 'default',
          text: 'Disabilita Prodotti',
          hint: 'Disabilita Prodotti',
          onClick() {
            var result = DevExpress.ui.dialog.confirm("<i>Confermi l'operazione su tutti i prodotti?</i>", "Disabilita tutti i Prodotti");
            result.done(function (dialogResult) {
              if (dialogResult) {
                _objRef.invokeMethodAsync('StatoProdotti', false)
                  .catch(err => console.error(err.toString()));
              }
            });
          },
        },
      },
      {
        location: 'before',
        widget: 'dxButton',
        options: {
          icon: 'fa-solid fa-circle-check',
          type: 'default',
          text: 'Abilita Prodotti',
          hint: 'Abilita Prodotti',
          onClick() {
            var result = DevExpress.ui.dialog.confirm("<i>Confermi l'operazione su tutti i prodotti?</i>", "Abilita tutti i Prodotti");
            result.done(function (dialogResult) {
              if (dialogResult) {
                _objRef.invokeMethodAsync('StatoProdotti', true)
                  .catch(err => console.error(err.toString()));
              }
            });
          },
        },
      },
      {
        location: 'before',
        widget: 'dxButton',
        options: {
          icon: 'fa-solid fa-globe',
          type: 'default',
          text: 'Aggiorna Stato Prodotti Cloud',
          hint: "Aggiorna Stato Prodotti Cloud",
          onClick() {
            _objRef.invokeMethodAsync('RefreshAnagrafiche', 1)
              .catch(err => console.error(err.toString()));
          },
        },
      },
      {
        location: 'before',
        widget: 'dxButton',
        options: {
          icon: 'fa-solid fa-globe',
          type: 'default',
          text: 'Aggiorna Anagrafica Prodotti Cloud',
          hint: "Aggiorna Anagrafica Prodotti Cloud",
          onClick() {
            _objRef.invokeMethodAsync('RefreshAnagrafiche', 2)
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