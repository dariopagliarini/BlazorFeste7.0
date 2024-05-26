"use strict";

export var GestioneAnagrClientsObj = {
  objRef: null,
  gridCasse: null,
  anagrClients: null,
  numeroRecords: 0,

  init: (_objRef, _anagrClients) => {
    console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneAnagrClientsObj - init");

    GestioneAnagrClientsObj.objRef = _objRef;
    GestioneAnagrClientsObj.anagrClients = _anagrClients;

    if (GestioneAnagrClientsObj.gridCasse) {
      GestioneAnagrClientsObj.gridCasse.dispose();
    }
    $("#myGridCasse").dxDataGrid({
      dataSource: _anagrClients,
      height: "100%",
      width: "100%",
      keyExpr: "indirizzoIP",
      editing: {
        mode: 'batch',
        allowAdding: true, 
        allowUpdating: true,
        selectTextOnEditStart: true,
        startEditAction: 'click',
      },
      columns: [
        {
          caption: "Indirizzo", dataField: "indirizzoIP", alignment: "center",
          width: 150,
        },
        {
          caption: "Livello", dataField: "livello", alignment: "center",
          width: 60,
        }
      ],
      noDataText: "Nessun Dato Disponibile",
      focusedRowEnabled: false,
      showRowLines: true,
      rowAlternationEnabled: true,
      columnAutoWidth: true,
      wordWrapEnabled: true,
      showBorders: true,
      hoverStateEnabled: true,
      onInitialized(e) {
        GestioneAnagrClientsObj.gridCasse = e.component;
      },
      onCellPrepared: function (e) {
        if (e.rowType === "data" && (["backColor", "foreColor"].indexOf(e.column.dataField) > -1) ) {
          e.cellElement.css("background-color", e.data.backColor);
          e.cellElement.css("color", e.data.foreColor);
        }
      },
      onRowPrepared: function (e) {
        switch (e.rowType) {
          case "header":
            break;

          case "data":
            e.rowElement.css({ height: "42px" });
            e.rowElement.find("td").css({ "vertical-align": "middle" });
            break;
        }
      },
      onInitNewRow: function (e) {
        e.data.idCassa = 90 + GestioneAnagrClientsObj.numeroRecords;
        GestioneAnagrClientsObj.numeroRecords++;

        e.data.indirizzoIP = "127.0.0.1";
        e.data.livello = 0;
      },
      onSaving(e) {
        if (e.changes.length) {
          GestioneAnagrClientsObj.objRef.invokeMethodAsync('BatchUpdateRequest', e.changes)
            .then((_newanagrClients) => {
              GestioneAnagrClientsObj.anagrClients = _newanagrClients;
              GestioneAnagrClientsObj.gridCasse.option("dataSource", _newanagrClients);

              e.component.refresh(true).done(() => {
                e.component.cancelEditData();
                GestioneAnagrClientsObj.numeroRecords = 0;
              });
            });
        }
      },
    });
  },

  dispose: () => {
    try {
      GestioneAnagrClientsObj.gridCasse.dispose();

      console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneAnagrClientsObj - dispose");
    } catch (error) {
      console.error(moment().format("HH:mm:ss.SSS") + " - " + " GestioneAnagrClientsObj - dispose - Error: " + error);
    }
  }
};

