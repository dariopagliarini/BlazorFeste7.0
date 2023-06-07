"use strict";

export async function downloadFileFromStream(fileName, contentStreamReference) {
  const blob = new Blob([contentStreamReference]);
  const url = URL.createObjectURL(blob);

  triggerFileDownload(fileName, url);

  URL.revokeObjectURL(url);
}

function triggerFileDownload(fileName, url) {
  const anchorElement = document.createElement('a');
  anchorElement.href = url;
  anchorElement.download = fileName ?? '';
  anchorElement.click();
  anchorElement.remove();
}

export var RapportiObj = {
  objRef: null,

  loadPanel: null,

  Rapporti_selFesta: null,
  Rapporti_strAllSelected: null,

  Rapporti_gridFeste: null,
  Rapporti_gridGiornate: null,
  Rapporti_gridOrdini: null,

  toolbar: null,

  idFesta: null,
  strAllSelected: null,

  renderGrids: (_objRef, dati) => {
    RapportiObj.objRef = _objRef;

    $("#Rapporti_myGridFeste").dxDataGrid({
      dataSource: dati,
      keyExpr: "idFesta",
      height: "185px",
      noDataText: "",
      focusedRowEnabled: false,
      showRowLines: true,
      rowAlternationEnabled: true,
      columnAutoWidth: true,
      wordWrapEnabled: true,
      showBorders: true,
      hoverStateEnabled: true,

      columns: [
        {
          dataField: "idFesta",
          visible: false
        }, {
          dataField: "festa",
          caption: "Descrizione"
        }, 
        {
          caption: "Associazione",
          dataField: "associazione",
          alignment: "left",
        },
        {
          caption: "Data Inizio",
          dataField: "dataInizio",
          alignment: "center",
          width: 120,
        },
        {
          caption: "Data Fine",
          dataField: "dataFine",
          alignment: "center",
          width: 120,
          visible: false
        },
      ],

      //selectionFilter: ['idFesta', '=', festa.idFesta.toString()],
      selection: {
        mode: 'single',
        //deferred: true,
      },
      paging: {
        pageSize: 10,
      },
      filterRow: {
        visible: false,
      },
      onInitialized(e) {
        RapportiObj.Rapporti_gridFeste = e.component;
      },
      onSelectionChanged(selectedItems) {
        const data = selectedItems.selectedRowsData[0];
        if (data) {
          RapportiObj.idFesta = data.idFesta;

          RapportiObj.Rapporti_gridGiornate.beginUpdate();
          RapportiObj.Rapporti_gridGiornate.option("dataSource", data.giornate);
          RapportiObj.Rapporti_gridGiornate.endUpdate();
        } else {
          RapportiObj.idFesta = -1;
        }
        RapportiObj.Rapporti_gridOrdini.option("dataSource", []);
        $("#btnEsportaReport").dxButton("instance").option("disabled", true);
      },
    });

    $("#Rapporti_myGridGiornate").dxDataGrid({
      dataSource: [],
      keyExpr: "dataAssegnazione_Ticks",
      height: "100%",
      noDataText: "Nessuna Festa Selezionata",
      focusedRowEnabled: false,
      showRowLines: true,
      rowAlternationEnabled: true,
      columnAutoWidth: true,
      wordWrapEnabled: true,
      showBorders: true,
      hoverStateEnabled: true,

      columns: [
        {
          dataField: "dataAssegnazione_Ticks",
          visible: false
        }, 
        {
          caption: "Giornata",
          dataField: "dataAssegnazione",
          alignment: "left",
          //width: 120,
        }
      ],
      selectionFilter: ['dataAssegnazione_Ticks', '=', '*'],
      selection: {
        mode: 'multiple',
        //deferred: true,
      },
      paging: {
        pageSize: 15,
      },
      filterRow: {
        visible: false,
      },
      onInitialized(e) {
        RapportiObj.Rapporti_gridGiornate = e.component;
        //calculateStatistics();
      },
      onSelectionChanged(selectedItems) {
        const rowData = selectedItems.selectedRowsData;
        if (rowData.length > 0) {
          RapportiObj.strAllSelected = DevExpress.data.query(rowData).select("dataAssegnazione_Ticks").toArray().map(m => m["dataAssegnazione_Ticks"]);
        } else {
          RapportiObj.strAllSelected = "";
        }
        RapportiObj.Rapporti_gridOrdini.option("dataSource", []);
        $("#btnEsportaReport").dxButton("instance").option("disabled", true);
        $("#btnGeneraReport").dxButton("instance").option("disabled", RapportiObj.strAllSelected === "");
      },
    });

    $('#Rapporti_toolbar').dxToolbar({
      items: [
        {
          location: 'after',
          widget: 'dxButton',
          options: {
            elementAttr: { id: 'btnGeneraReport' },
            disabled: true,
            icon: 'fa-solid fa-arrows-rotate',
            type: 'default',
            text: 'Genera Report',
            hint: "Genera Report Consumi",
            onClick() {
              RapportiObj.objRef.invokeMethodAsync('onAggiornaStatoProdottiAsync', RapportiObj.idFesta, RapportiObj.strAllSelected)
                .then(_retvalue => {
                  var retvalue = JSON.parse(_retvalue);

                  if ("statoOrdini" in retvalue) {
                    var ds = retvalue["statoOrdini"];

                    var totalItems = [];
                    var colonne = [
                      {
                        caption: "#", dataField: "idProdotto", alignment: "center", width: 30,
                      },
                      {
                        caption: "Prodotto", dataField: "nomeProdotto", alignment: "left", name: "Prodotto",
                      },
                      {
                        caption: "Qtà", dataField: "quantità", width: 50,
                      },
                      {
                        caption: "Importo", dataField: "importo", format: { type: "currency", currency: "EUR", precision: 2 }, width: 120,
                      },
                    ];

                    if (ds.length > 0) {
                      totalItems = [
                        { column: 'quantità', summaryType: 'sum', valueFormat: { type: "fixedPoint", precision: 0 }, displayFormat: "{0}" },
                        { column: 'importo', summaryType: 'sum', valueFormat: { type: "currency", currency: "EUR", precision: 2 }, displayFormat: "{0}" },
                      ];

                      let idx = 0;
                      ds[0].statoCassa.forEach(cassa => {
                        let _cassa = retvalue["statoCasse"].find(({ idCassa }) => idCassa === cassa.idCassa);

                        let colonneCassa = [
                          { caption: "Qtà", name: "c" + idx + "_Qta", dataField: 'statoCassa[' + idx + '].quantitàProdotto', format: { type: "fixedPoint", precision: 0 }, width: 55, },
                          { caption: "Importo", name: "c" + idx + "_Imp", dataField: 'statoCassa[' + idx + '].importo', format: { type: "currency", currency: "EUR", precision: 2 }, width: 90, },
                        ];

                        if (_cassa) {
                          colonne.push({ caption: _cassa.cassa, name: "cassa" + idx, visible: true, alignment: "center", columns: colonneCassa });
                        } else {
                          colonne.push({ caption: "Cassa #" + idx, name: "cassa" + idx, visible: true, alignment: "center", columns: colonneCassa });
                        }

                        totalItems.push({ column: 'c' + idx + '_Qta', summaryType: 'sum', valueFormat: { type: "fixedPoint", precision: 0 }, displayFormat: "{0}" });
                        totalItems.push({ column: 'c' + idx + '_Imp', summaryType: 'sum', valueFormat: { type: "currency", currency: "EUR", precision: 2 }, displayFormat: "{0}" });

                        idx++;
                      });
                    }
                    $("#btnEsportaReport").dxButton("instance").option("disabled", ds.length === 0);

                    RapportiObj.Rapporti_gridOrdini.beginUpdate();
                    RapportiObj.Rapporti_gridOrdini.option("columns", colonne);
                    RapportiObj.Rapporti_gridOrdini.option("summary.totalItems", totalItems);
                    RapportiObj.Rapporti_gridOrdini.option("dataSource", ds); //retvalue["statoOrdini"]);
                    RapportiObj.Rapporti_gridOrdini.endUpdate();
                  }
                });
            },
          },
        },
        {
          location: 'after',
          widget: 'dxButton',
          options: {
            elementAttr: { id: 'btnEsportaReport' },
            disabled: true,
            icon: 'fa-solid fa-file-excel',
            type: 'default',
            text: 'Esporta Report',
            hint: "Esporta Report Consumi",
            onClick() {
              RapportiObj.objRef.invokeMethodAsync('onExportingAsync', RapportiObj.idFesta, RapportiObj.strAllSelected)
                .catch(err => console.error(err.toString()));
            },
          },
        },
      ],
      onInitialized(e) {
        RapportiObj.toolbar = e.component;
      },
    });

    $("#Rapporti_myGridOrdini").dxDataGrid({
      dataSource: [],
      height: "100%",
      width: "100%",
      keyExpr: "idProdotto",
      columns: [
        {
          caption: "#", dataField: "idProdotto", alignment: "center", width: 30,
        },
        {
          caption: "Prodotto", dataField: "nomeProdotto", alignment: "left", name: "Prodotto",
        },
        {
          caption: "Qtà", dataField: "quantità", width: 50,
        },
        {
          caption: "Importo", dataField: "importo", format: { type: "currency", currency: "EUR", precision: 2 }, width: 120,
        },
      ],
      noDataText: "Nessuna Selezione Applicata",
      focusedRowEnabled: false,
      showRowLines: true,
      rowAlternationEnabled: true,
      columnAutoWidth: true,
      wordWrapEnabled: false,
      showBorders: true,
      hoverStateEnabled: true,
      paging: { enabled: false },
      scrolling: {
        mode: "standard" // or "virtual" | "infinite"
      },
      onInitialized(e) {
        RapportiObj.Rapporti_gridOrdini = e.component;
      },
      onRowPrepared: function (e) {
        switch (e.rowType) {
          case "header":
            break;

          case "data":
            //e.rowElement.css({ height: "42px" });
            e.rowElement.find("td").css({ "vertical-align": "middle" });
            break;
        }
      },
    });
  },

//#region LoadPanel
  loadPanelRender: (container) => {
    RapportiObj.loadPanel = $(container).dxLoadPanel({
      message: "Creazione report in corso...",
      visible: false,
      showIndicator: true,
      showPane: true,
      hideOnOutsideClick: false
    }).dxLoadPanel("instance");
  },

  loadPanelSetVisible: (visible) => {
    if (visible) {
      RapportiObj.loadPanel.show();
    } else {
      RapportiObj.loadPanel.hide();
    }
  },

  loadPanelSetMessage: (_message) => {
    RapportiObj.loadPanel.option("message", _message);
  },
//#endregion

//#region File Download
  downloadFileFromStream : (fileName, contentStreamReference)  => {
    const blob = new Blob([contentStreamReference]);
    const url = URL.createObjectURL(blob);

    RapportiObj.triggerFileDownload(fileName, url);

    URL.revokeObjectURL(url);
  },

  triggerFileDownload : (fileName, url) => {
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
  },
//#endregion
}

