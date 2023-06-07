"use strict";

export var GestioneAnagrCasseObj = {
  objRef: null,
  gridCasse: null,
  anagrCasse: null,
  numeroRecords: 0,

  init: (_objRef, _anagrCasse) => {
    console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneAnagrCasseObj - init");

    GestioneAnagrCasseObj.objRef = _objRef;
    GestioneAnagrCasseObj.anagrCasse = _anagrCasse;

    if (GestioneAnagrCasseObj.gridCasse) {
      GestioneAnagrCasseObj.gridCasse.dispose();
    }
    $("#myGridCasse").dxDataGrid({
      dataSource: _anagrCasse,
      height: "100%",
      width: "100%",
//      keyExpr: ["idListino", "idCassa"],
      keyExpr: "idPrimaryKey",
      editing: {
        mode: 'batch',
        allowAdding: true, 
        allowUpdating: true,
        selectTextOnEditStart: true,
        startEditAction: 'click',
      },
      columns: [
        {
          caption: "Id. Cassa", dataField: "idCassa", alignment: "center",
          allowEditing: false,
          width: 50,
        },
        {
          caption: "Abilitata", dataField: "abilitata", alignment: "center",
          width: 60,
          dataType: "boolean",
        },
        {
          caption: "Visibile", dataField: "visibile", alignment: "center",
          width: 60,
          dataType: "boolean",
        },
        {
          caption: "Cassa" , dataField: "cassa", alignment: "left",
        },
        {
          caption: "Porta", dataField: "portName", alignment: "left",
        },
        {
          caption: "E' remota", dataField: "isRemote", alignment: "center",
          width: 60,
          dataType: "boolean",
        },
        {
          caption: "Indirizzo Remoto", dataField: "remoteAddress", alignment: "left",
        },
        {
          caption: "Range Prodotti", dataField: "prodotti", alignment: "left",
        },
        {
          caption: "Colore",
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
        {
          caption: "Stampa Scontrino",
          columns: [
            {
              dataType: "boolean",
              caption: "Abilitato", dataField: "scontrinoAbilitato", alignment: "center",
              width: 60,
            },
            {
              dataType: "boolean",
              caption: "Solo Banco", dataField: "soloBanco", alignment: "center",
              width: 60,
            },
            {
              dataType: "boolean",
              caption: "Muto", dataField: "scontrinoMuto", alignment: "center",
              width: 60,
            },
          ]
        },
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
        GestioneAnagrCasseObj.gridCasse = e.component;
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
        e.data.idCassa = 90 + GestioneAnagrCasseObj.numeroRecords;
        GestioneAnagrCasseObj.numeroRecords++;

        e.data.abilitata = false;
        e.data.visibile = false;
        e.data.cassa = "Cassa ???";
        e.data.portName = "COM3";
        e.data.isRemote = false;
        e.data.remoteAddress = "";
        e.data.prodotti = "1-70";

        e.data.backColor = "#e95b54";
        e.data.scontrinoAbilitato = false;
        e.data.soloBanco = false;
        e.data.scontrinoMuto = false;
      },
      onSaving(e) {
        if (e.changes.length) {
          GestioneAnagrCasseObj.objRef.invokeMethodAsync('BatchUpdateRequest', e.changes)
            .then((_newAnagrCasse) => {
              GestioneAnagrCasseObj.anagrCasse = _newAnagrCasse;
              GestioneAnagrCasseObj.gridCasse.option("dataSource", _newAnagrCasse);

              e.component.refresh(true).done(() => {
                e.component.cancelEditData();
                GestioneAnagrCasseObj.numeroRecords = 0;
              });
            });
        }
      },
    });
  },

  dispose: () => {
    try {
      GestioneAnagrCasseObj.gridCasse.dispose();

      console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneAnagrCasseObj - dispose");
    } catch (error) {
      console.error(moment().format("HH:mm:ss.SSS") + " - " + " GestioneAnagrCasseObj - dispose - Error: " + error);
    }
  }
};

