"use strict";

export var TopBarObj = {
  objRef: null,
  ToolBar: null,
  Liste: null,

  init: (_objRef, _Liste, _IsDevelopment, _clientInfo) => {
    console.log(moment().format("HH:mm:ss.SSS") + " - " + " TopBarObj - init - " + _clientInfo.ipAddress);
    TopBarObj.objRef = _objRef;
    TopBarObj.Liste = _Liste;

    $("#myToolbar").dxToolbar({
      items: [
        {
          location: 'before',
          widget: 'dxSelectBox',
          options: {
            width: 180,
            elementAttr: { id: "listaSelectBox" },
            placeholder: "Lista...",
            items: TopBarObj.Liste,
            valueExpr: 'idLista',
            displayExpr: 'lista',
            onValueChanged(args) {
              if (args.value != null) {
                let currentUrl = window.location.href;
                let navigateToUrl = "GestioneLista/" + args.value;
                if (currentUrl.indexOf(navigateToUrl) == -1) 
                  _objRef.invokeMethodAsync("NavigateToPage", navigateToUrl)
                    .catch(err => console.error(err.toString()));
              }
            },
          },
        },
        { 
          location: 'before',
          widget: 'dxButton',
          options: {
            icon: 'fa-solid fa-cart-shopping',
            type: 'default',
            text: 'Ordini',
            hint: "Pagina Gestione Ordini",
            onClick() {
              _objRef.invokeMethodAsync("NavigateToPage", "ElencoOrdini")
                .catch(err => console.error(err.toString()));
            },
          },
        },
        {
          location: 'before',
          widget: 'dxButton',
          options: {
            icon: 'fa-solid fa-file-export',
            type: 'default',
            text: 'Rapporti',
            hint: "Pagina Gestione Rapporti",
            onClick() {
              _objRef.invokeMethodAsync("NavigateToPage", "Rapporti")
                .catch(err => console.error(err.toString()));
            },
          },
        },
        {
          location: 'before',
          widget: 'dxButton',
          visible: _IsDevelopment,
          options: {
            icon: 'fa-solid fa-person-digging',
            type: 'default',
            text: 'Servizio',
            hint: "Pagina servizio",
            onClick() {
              _objRef.invokeMethodAsync("NavigateToPage", "Servizio")
                .catch(err => console.error(err.toString()));
            },
          },
        },
      ],
      onInitialized(e) {
        TopBarObj.ToolBar = e.component;
      },
    });
  },

  dispose: () => {
    $("#myToolbar").dxToolbar("dispose");
    $("#myToolbar").remove();

    console.log(moment().format("HH:mm:ss.SSS") + " - " + " TopBarObj - dispose");
  },
};

function resyncSelectBox(id) {
  if ($("#listaSelectBox")?.dxSelectBox("instance")?.option() != undefined)
    $("#listaSelectBox")?.dxSelectBox("instance")?.option('value', id);
};
export { resyncSelectBox };
