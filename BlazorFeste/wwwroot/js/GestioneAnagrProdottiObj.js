"use strict";

export var GestioneAnagrProdottiObj = {
  objRef: null,
  gridProdotti: null,
  anagrProdotti: null,

  init: (_objRef, _anagrProdotti) => {
    console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneAnagrProdottiObj - init");

    GestioneAnagrProdottiObj.objRef = _objRef;
    GestioneAnagrProdottiObj.anagrProdotti = _anagrProdotti;

    if (GestioneAnagrProdottiObj.gridProdotti) {
      GestioneAnagrProdottiObj.gridProdotti.dispose();
    }
    $("#myGridProdotti").dxDataGrid({
      dataSource: _anagrProdotti,
      height: "100%",
      width: "100%",
      keyExpr: "idProdotto",
      columns: [
        {
          caption: "Id. Prodotto", dataField: "idProdotto", alignment: "center",
          allowEditing: false,
          allowFiltering: false,
          showInColumnChooser: false,
          width: 70,
        },
        {
          caption: "Prodotto", dataField: "nomeProdotto", alignment: "left",
          showInColumnChooser: false,
        },
        {
          caption: "Prezzo Unitario", dataField: "prezzoUnitario", alignment: "right", format: { type: "currency", currency: "EUR", precision: 2 },
          allowFiltering: false,
          showInColumnChooser: false,
          width: 80,
        },
        {
          caption: "Stato", dataField: "stato", alignment: "center",
          width: 80,
          showInColumnChooser: false,
          dataType: "boolean",
        },
        {
          caption: "Lista", dataField: "idLista", alignment: "center",
          width: 80,
        },
        {
          dataType: "number",
          editorOptions: {
            min: 0,
            showSpinButtons: true,
          },
          caption: "Magazzino", dataField: "magazzino", alignment: "right",
          width: 100,
        },
        {
          caption: "Consumo",
          columns: [
            {
              caption: "Prodotto", dataField: "consumo", alignment: "right",
              allowFiltering: false,
              allowEditing: false,
              visible:false,
              width: 60,
            },
            {
              caption: "Cumul.", dataField: "consumoCumulativo", alignment: "right",
              allowFiltering: false,
              allowEditing: false,
              width: 60,
            },
          ]
        },
        {
          caption: "Evaso",
          visible: false,
          columns: [
            {
              caption: "Prodotto", dataField: "evaso", alignment: "right",
              allowFiltering: false,
              allowEditing: false,
              width: 60,
            },
            {
              caption: "Cumul.", dataField: "evasoCumulativo", alignment: "right",
              allowFiltering: false,
              allowEditing: false,
              width: 60,
            },
          ]
        },
        {
          caption: "Evadi Su IdProdotto", dataField: "evadiSuIdProdotto", alignment: "center",
          width: 100,
        },
        {
          caption: "Stampa Ticket Coda", dataField: "printQueueTicket", alignment: "center",
          dataType: "boolean",
          width: 90,
        },
        {
          caption: "Etichetta 'Da  Evadere'", dataField: "viewLableDaEvadere", alignment: "center",
          dataType: "boolean",
          width: 90,
        },
        {
          caption: "Colore",
          visible: false,
          columns: [
            {
              caption: "Sfondo", dataField: "backColor", alignment: "center",
              width: 120,
            },
            {
              caption: "Testo", dataField: "foreColor", alignment: "center",
              width: 120,
            },
          ]
        },
      ],
      noDataText: "Nessun Dato Disponibile",
      focusedRowEnabled: false,
      showRowLines: true,
      rowAlternationEnabled: true,
      paging: { enabled: false },
      scrolling: {
        mode: "standard" // or "virtual" | "infinite"
      },
      headerFilter: { visible: true },
      filterRow: {
        visible: false,
      },
      columnAutoWidth: true,
      wordWrapEnabled: true,
      showBorders: true,
      hoverStateEnabled: true,
      columnHidingEnabled: true,
      columnChooser: {
        enabled: true,
        mode: 'select',
        position: {
          my: 'right top',
          at: 'right bottom',
          of: '.dx-datagrid-column-chooser-button',
        },
        search: {
          enabled: true,
          editorOptions: { placeholder: 'Search column' },
        },
        selection: {
          recursive: true,
          selectByClick: true,
          allowSelectAll: true,
        },
      },
      //columnChooser: {
      //  enabled: true,
      //  mode: 'select',
      //},
      onInitialized(e) {
        GestioneAnagrProdottiObj.gridProdotti = e.component;
      },
      onCellPrepared: function (e) {
        if (e.rowType === "data" && (["backColor", "foreColor"].indexOf(e.column.dataField) > -1)) {
          e.cellElement.css("background-color", e.data.backColor);
          e.cellElement.css("color", e.data.foreColor);
        }
      },
      toolbar: {
        items: [
          {
            location: 'after',
            name: 'saveButton',
          },
          {
            location: 'after',
            name: 'revertButton',
          },
          {
            location: 'after',
            name: 'columnChooserButton',
          },
        ],
      },
      editing: {
        mode: 'batch',
        allowAdding: false,
        allowUpdating: true,
        allowDeleting: false,
        selectTextOnEditStart: true,
        startEditAction: 'click',
      },
      onSaving(e) {
        if (e.changes.length) {
          GestioneAnagrProdottiObj.objRef.invokeMethodAsync('BatchUpdateRequest', e.changes)
            .then(() => {
              e.component.refresh(true).done(() => {
                e.component.cancelEditData();
              });
            }
          ).catch(err => console.error(err.toString()));
        }
      },
    });
  },

  dispose: () => {
    try {
      GestioneAnagrProdottiObj.gridProdotti.dispose();
      console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneAnagrProdottiObj - dispose");
    } catch (error) {
      console.error(moment().format("HH:mm:ss.SSS") + " - " + " GestioneAnagrProdottiObj - dispose - Error: " + error);
    }
  },
};

